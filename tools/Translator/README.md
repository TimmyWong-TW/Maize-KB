# Translator

`/translate/<src>/<to>`

Example:
```http
POST /translate/zh/ja
Content-Type: application/json

{"input":["第一句。","第二段。","第三片。"]}
```

## [M2M100 Languages](https://huggingface.co/facebook/m2m100_1.2B#languages-covered)

- `zh` 中文
- `en` English
- `ko` 南韓한굴
- `ja` 日本語
- `th` 泰文
- `id` 印尼文
- `sw` 史瓦希利文
- `ne` 尼泊爾天城文
- `el` 希臘文
- `he` 希伯來文
- `fr` 法文
- `de` 德文
- `km` 高棉文
- ……