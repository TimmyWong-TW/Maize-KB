# Translator

`/translate/<src>/<to>`

Example:
```http
POST /translate/cmn_Hant/jpn
Content-Type: application/json

{"input":["第一句。","第二段。","第三片。"]}
```

## [Seamless M4T v2 Languages](https://huggingface.co/facebook/seamless-m4t-v2-large#supported-languages)

- `cmn_Hant` 繁體中文
- `yue` 粵語漢字
- `cmn` 残体中文
- `eng` English
- `kor` 南韓한굴
- `jpn` 日本語
- `tha` 泰文
- `ind` 印尼文
- `swh` 史瓦希利文
- `npi` 尼泊爾天城文
- `ell` 希臘文
- `heb` 希伯來文
- `fra` 法文
- `deu` 德文
- `khm` 高棉文
- ……