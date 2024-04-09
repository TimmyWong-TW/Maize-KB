# Descriptor

Generate an overview for a sermon, speech, or long text.
Outline from the output may then be used for semantic chunking.

Include API key for Google Generative AI in `tools/secrets.env` as `GOOGLE_AI_API_KEY` when using with Docker Compose.

```sh
cd tools
docker compose --rm -d descriptor
```

## Exceptions

Some content could not pass the moderation of Google AI, or otherwise causes empty output.

Refer to `overview/error.log` for files that requires manual actions. Microsoft Copilot or another generative AI tool may be used manually.