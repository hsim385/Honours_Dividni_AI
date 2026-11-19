from google import genai
from google.genai import types
import pathlib
import json
import sys
import zipfile
import io
import os
import asyncio
import zipfile
import tempfile
from ollama import chat
from pathlib import Path
# importing required modules
from pypdf import PdfReader
import fitz  # pip install --upgrade pip; pip install --upgrade pymupdf
from tqdm import tqdm  # pip install tqdm

sys.stdout.reconfigure(encoding="utf-8")

async def callGemini(filepath, prompt):
    async with genai.Client().aio as client:
        size = os.path.getsize(filepath)


        async def uploadfiles(files):
            return await asyncio.gather(
                *(client.files.upload(file=file) for file in files)
            )


        async def getresponse(contents):
            return await client.models.generate_content(
                model="gemini-3.5-flash",
                contents=contents)

        if size / (1024 ** 2) < 50 and filepath.suffix.lower() == ".pdf":
            contents = [
                types.Part.from_bytes(
                    data=filepath.read_bytes(),
                    mime_type='application/pdf',
                ),
                prompt]
            response = await getresponse(contents)

        elif filepath.suffix.lower() == ".zip":
            with tempfile.TemporaryDirectory() as temp_dir:
                with zipfile.ZipFile(filepath) as z:
                    z.extractall(temp_dir)
                files = []
                for r, d, f in os.walk(temp_dir):
                    for file in f:
                        if file.endswith(".pdf"):
                            files.append(os.path.join(r, file))
                fileIDs = await uploadfiles(files)
                content = fileIDs + [prompt]
                response = await getresponse(content)

        else:
            myfile = await uploadfiles([filepath])
            print(myfile)
            content = myfile + [prompt]
            response = await getresponse(content)
        print(response.text)
        
async def callLocalAI(filepath, prompt):
    # creating a pdf reader object
    reader = PdfReader(filepath)
    # printing number of pages in pdf file
    print("page no:", len(reader.pages))
    pdfData = [reader.pages[i].extract_text() for i in range(len(reader.pages))]
    print(pdfData)

    root = Path(filepath).parent
    images_dir = root / "images"
    images_dir.mkdir(parents=True, exist_ok=True)
    
    workdir = os.path.join(root, "images")
    images = []
    for each_path in os.listdir(root):
        if Path(filepath).name in each_path:
            doc = fitz.Document((os.path.join(root, each_path)))
    
            for i in tqdm(range(len(doc)), desc="pages"):
                for img in tqdm(doc.get_page_images(i), desc="page_images"):
                    xref = img[0]
                    image = doc.extract_image(xref)
                    pix = fitz.Pixmap(doc, xref)
                    pix.save(os.path.join(workdir, "%s_p%s-%s.png" % (each_path[:-4], i, xref)))
                    images.append(os.path.join(workdir, "%s_p%s-%s.png" % (each_path[:-4], i, xref)))
    
    print("Done!")
    
    response = chat(
        model='gemma4',
        messages=[
            {
                'role': 'user',
                'content': f"""--------------------------------------------------------------------
          USER PROVIDED LECTURE MATERIAL
          --------------------------------------------------------------------
          
          you have been provided this text extracted from a pdf, grouped by pages, as the lecture material:  
          {pdfData}
          
          there may also be pictures from the pdf provided as well, you are expected to take their contents into consideration.
          
          
          {prompt}
        
          
          --------------------------------------------------------------------
          USER PROVIDED PROMPT
          --------------------------------------------------------------------
          please make a single question on quorums in the Dividni format""",
                'images': images,
            }
        ],
        options={
            "num_predict": 2048,
            "num_ctx": 32768,
        },
    )
    
    for file in images:
        os.remove(file)
    
    print(response.message.content)
    print("End")

async def main():

    # Read JSON from stdin
    raw_input = sys.stdin.read()

    data = json.loads(raw_input)

    filename = data["filename"]
    prompt = data["prompt"]
    AIModel = data["model"]

    # Retrieve the PDF
    filepath = pathlib.Path(__file__).parent.parent / "wwwroot" / "uploads" / filename
    
    if (AIModel == "gemini"):
        await callGemini(filepath, prompt)
    elif (AIModel == "localAI"):
        await callLocalAI(filepath, prompt)
        
asyncio.run(main())
