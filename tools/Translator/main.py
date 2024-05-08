from flask import Flask, request, Response
from transformers import M2M100Tokenizer, M2M100ForConditionalGeneration
import json
import torch

device = 'cuda' if torch.cuda.is_available() else 'cpu'
model_name = 'facebook/m2m100_1.2B'
app = Flask(__name__)
app.json.sort_keys = False
tokenizer = M2M100Tokenizer.from_pretrained(model_name)
model = M2M100ForConditionalGeneration.from_pretrained(model_name).to(device)

@app.route('/translate/<src>/<to>', methods=['POST'])
def translate(src, to):
    input = request.json['input']

    def generate():
        tokenizer.src_lang = src
        yield '{"object":"list","model":"' + model_name + '","data":['
        sub = False
        for i in input if isinstance(input, list) else [input]:
            if sub:
                yield ','
            sub = True
            inputs = tokenizer(i, return_tensors='pt').to(device)
            one = tokenizer.batch_decode(model.generate(**inputs, forced_bos_token_id=tokenizer.get_lang_id(to)), skip_special_tokens=True)[0]
            yield json.dumps(one)
        yield ']}'

    return Response(generate(), mimetype='application/json')

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=80)