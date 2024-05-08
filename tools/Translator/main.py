from flask import Flask, request, Response
from transformers import AutoProcessor, SeamlessM4Tv2ForTextToText
import json
import torch

device = 'cuda' if torch.cuda.is_available() else 'cpu'
model_name = 'facebook/seamless-m4t-v2-large'
app = Flask(__name__)
app.json.sort_keys = False
processor = AutoProcessor.from_pretrained(model_name)
model = SeamlessM4Tv2ForTextToText.from_pretrained(model_name).to(device)

@app.route('/t2tt/<src>/<to>', methods=['POST'])
def t2tt(src, to):
    input = request.json['input']

    def generate():
        yield '{"object":"list","model":"' + model_name + '","data":['
        sub = False
        for i in input if isinstance(input, list) else [input]:
            if sub:
                yield ','
            sub = True
            inputs = processor(text = i, src_lang=src, return_tensors='pt').to(device)
            one = processor.batch_decode(model.generate(**inputs, tgt_lang=to), skip_special_tokens=True)[0]
            yield json.dumps(one)
        yield ']}'

    return Response(generate(), mimetype='application/json')

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=80)