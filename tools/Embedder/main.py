from flask import Flask, request, jsonify
from FlagEmbedding import BGEM3FlagModel, FlagReranker
import base64
import struct

embedding_model_name = 'BAAI/bge-m3'
reranking_model_name = 'BAAI/bge-reranker-v2-m3'
app = Flask(__name__)
app.json.sort_keys = False
embedder = BGEM3FlagModel(embedding_model_name, use_fp16=True)
reranker = FlagReranker(reranking_model_name, use_fp16=True)

@app.route('/v1/embeddings', methods=['POST'])
def embed():
    input = request.json['input']
    is_base64 = request.json.get('encoding_format', 'float') == 'base64'
    return jsonify({
        'object': 'list',
        'data': [
            {
                'object': 'embedding',
                'embedding': base64.b64encode(struct.pack('f'*len(vector), *vector)).decode('utf-8') if is_base64 else vector,
                'index': i
            }
            for i, vector in enumerate(embedder.encode(
                input if isinstance(input, list) else [input], batch_size=12, max_length=8192
            )['dense_vecs'].tolist())
        ],
        'model': embedding_model_name,
        'usage': {
            'prompt_tokens': 0,
            'total_tokens': 0
        }
    })

@app.route('/v1/relevances', methods=['POST'])
def rerank():
    input = request.json['input']
    return jsonify({
        'object': 'list',
        'data': [
            {
                'object': 'relevance',
                'score': score,
                'index': i
            }
            for i, score in enumerate(reranker.compute_score(
                input if isinstance(input[0], list) else [input], normalize=True
            ))
        ],
        'model': reranking_model_name
    })

if __name__ == '__main__':
    print('models should now be cached')