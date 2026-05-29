from google import genai
from google.genai import types
import pathlib
import json
import sys
import zipfile
import io
import os
import asyncio

async def main():
    print("start")
    async with genai.Client().aio as client:
        
        # Read JSON from stdin
        raw_input = sys.stdin.read()
        
        data = json.loads(raw_input)
        
        filename = data["filename"]
        prompt = data["prompt"]
        
        # Retrieve the PDF
        filepath = pathlib.Path(__file__).parent.parent / "wwwroot" / "uploads" / filename
        size = os.path.getsize(filepath)
        
        
        async def uploadfiles(files):
            return await asyncio.gather(
                *(client.files.upload(file=file) for file in files)
            )
            
            
        async def getresponse(contents):
            return await client.models.generate_content(
                model="gemini-3.5-flash",
                contents=contents)
        
        if size / (1024 ** 2) < 50:
            contents = [
                types.Part.from_bytes(
                    data=filepath.read_bytes(),
                    mime_type='application/pdf',
                ),
                prompt]
            response = await getresponse(contents)
            
        else:
            myfile = await uploadfiles([filename])
            response = await getresponse([prompt, myfile])
        print(response.text)

asyncio.run(main())