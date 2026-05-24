from google import genai
from google.genai import types
import pathlib
import json
import sys

# Read JSON from stdin
raw_input = sys.stdin.read()

data = json.loads(raw_input)

filename = data["filename"]
prompt = data["prompt"]

client = genai.Client()

# Retrieve and encode the PDF byte
script_dir = pathlib.Path(__file__).parent.parent

# Full path to file.pdf
filepath = script_dir / "wwwroot" / "uploads" / filename

prompt = prompt
response = client.models.generate_content(
  model="gemini-3.5-flash",
  contents=[
      types.Part.from_bytes(
        data=filepath.read_bytes(),
        mime_type='application/pdf',
      ),
      prompt])
print(response.text)