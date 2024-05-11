from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer
from sklearn.preprocessing import normalize
import base64
import json
import struct

embedding_model_name = 'infgrad/stella-mrl-large-zh-v3.5-1792d'
app = Flask(__name__)
app.json.sort_keys = False
embedder = SentenceTransformer(embedding_model_name)

def round12(value):
    return round(value, 9 if value < 0 else 10)

@app.route('/v1/embeddings', methods=['POST'])
def embed():
    input = request.json['input']
    is_base64 = request.json.get('encoding_format', 'float') == 'base64'
    dimensions = request.json.get('dimensions', 1792)
    trim = dimensions < 1792
    inputs = input if isinstance(input, list) else [input]
    # tokens = sum([len(embedder.tokenize(i)['input_ids']) for i in inputs])
    tokens = sum(len(i) for i in inputs)
    vectors = embedder.encode(
        inputs,
        normalize_embeddings=not trim
    )
    if trim:
        vectors = normalize(vectors[:, :dimensions])
    return jsonify({
        'object': 'list',
        'data': [
            {
                'object': 'embedding',
                'embedding': base64.b64encode(struct.pack('f'*len(vector), *vector)).decode('utf-8')
                    if is_base64 else [round12(f) for f in vector],
                'index': i
            }
            for i, vector in enumerate(vectors.astype(float))
        ],
        'model': embedding_model_name,
        'usage': {
            'prompt_tokens': tokens,
            'total_tokens': tokens
        }
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=80)