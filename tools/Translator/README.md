# Translator

`/translate/<src>/<to>`

Example:
```http
POST /translate/zho_Hant/jpn_Jpan
Content-Type: application/json

{"input":["第一句。","第二段。","第三片。"]}
```

## [Languages in FLORES-200](https://github.com/facebookresearch/flores/blob/main/flores200/README.md#languages-in-flores-200)

- `zho_Hant` 繁體中文
- `yue_Hant` 粵語漢字
- `zho_Hans` 残体中文
- `eng_Latn` English
- `kor_Hang` 南韓한굴
- `jpn_Jpan` 日本語
- `tha_Thai` 泰文
- `ind_Latn` 印尼文
- `swh_Latn` 史瓦希利文
- `npi_Deva` 尼泊爾天城文
- `ell_Grek` 希臘文
- `heb_Hebr` 希伯來文
- `fra_Latn` 法文
- `deu_Latn` 德文
- `khm_Khmr` 高棉文
- ……