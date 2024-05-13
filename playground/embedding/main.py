from flask import Flask, request, Response
from FlagEmbedding import BGEM3FlagModel
import base64
import json
import struct

embedding_model_name = 'BAAI/bge-m3'
app = Flask(__name__)
app.json.sort_keys = False
embedder = BGEM3FlagModel(embedding_model_name, use_fp16=True)

def round12(value):
    return round(value, 9 if value < 0 else 10)

@app.route('/v1/embeddings', methods=['POST'])
def embed():
    input = request.json['input']
    is_base64 = request.json.get('encoding_format', 'float') == 'base64'
    embeddings = embedder.encode(
        input if isinstance(input, list) else [input],
        batch_size=8,
        max_length=8192,
        return_colbert_vecs=True
    )
    vectors = embeddings['dense_vecs'].tolist()
    tokens = sum([len(v) for v in embeddings['colbert_vecs']])
    def generate():
        t = str(tokens)
        yield '{"object":"list","data":['
        sub = False
        for i, vector in enumerate(vectors):
            if sub:
                yield ','
            sub = True
            yield json.dumps({
                'object': 'embedding',
                'embedding': base64.b64encode(struct.pack('f'*len(vector), *vector)).decode('utf-8')
                    if is_base64 else [round12(f) for f in vector],
                'index': i
            }, ensure_ascii=False)
        yield '],"model":' + json.dumps(embedding_model_name) + ',"usage":{"prompt_tokens":' + t + ',"total_tokens":' + t + '}}'
    return Response(generate(), mimetype='application/json')

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=80)