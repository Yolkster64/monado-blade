# Python FastAPI LLM API Template
from fastapi import FastAPI
from pydantic import BaseModel
import openai

app = FastAPI()

class Query(BaseModel):
    prompt: str

@app.post("/generate")
def generate(query: Query):
    # Replace with your OpenAI/Azure API call
    response = openai.Completion.create(
        engine="davinci",
        prompt=query.prompt,
        max_tokens=100
    )
    return {"result": response.choices[0].text}
