from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer

model_name = 'amu/tao-8k'
app = Flask(__name__)
app.json.sort_keys = False
model = SentenceTransformer(model_name)

@app.route('/v1/embeddings', methods=['POST'])
def encode():
    input = request.json['input']
    embeddings = model.encode(input if isinstance(input, list) else [input])
    return jsonify({
        'object': 'list',
        'data': embeddings.tolist(),
        'model': model_name,
        'usage': {
            'prompt_tokens': 0,
            'total_tokens': 0
        }
    })

if __name__ == '__main__':
    print('model should now be cached')