# Translator

`/translate/<src>/<to>`

Example:
```http
POST /translate/zh/ja
Content-Type: application/json

{"input":["第一句。","第二段。","第三片。"]}
```

## [mBART-50 Languages](https://huggingface.co/facebook/mbart-large-50-many-to-many-mmt#languages-covered)

- `zh_CN` 中国中文
- `en_XX` English
- `ko_KR` 南韓한굴
- `ja_XX` 日本語
- `th_TH` 泰文
- `id_ID` 印尼文
- `sw_KE` 肯亞史瓦希利文
- `ne_NP` 尼泊爾天城文
- `he_IL` 希伯來文
- `fr_XX` 法文
- `de_DE` 德文
- `km_KH` 高棉文
- ……