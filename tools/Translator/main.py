from flask import Flask, request, Response
from transformers import AutoModelForSeq2SeqLM, AutoTokenizer
import json
import torch

device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
model_name = 'facebook/nllb-200-1.3B'
app = Flask(__name__)
app.json.sort_keys = False
model = AutoModelForSeq2SeqLM.from_pretrained(model_name).to(device)

@app.route('/translate/<src>/<to>', methods=['POST'])
def translate(src, to):
    tokenizer = AutoTokenizer.from_pretrained(model_name, src_lang=src)
    input = request.json['input']

    def generate():
        yield '{"object":"list","model":"'
        yield model_name
        yield '","data":['
        sub = False
        for i in input if isinstance(input, list) else [input]:
            if sub:
                yield ','
            sub = True
            one = tokenizer.batch_decode(model.generate(**tokenizer(i, return_tensors='pt', padding=True).to(device), forced_bos_token_id=tokenizer.lang_code_to_id[to]), skip_special_tokens=True)[0]
            print(one)
            yield json.dumps(one)
        yield ']}'

    return Response(generate(), mimetype='application/json')

if __name__ == '__main__':
    print('models should now be cached')