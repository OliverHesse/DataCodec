# DataCodec

Format-independent reading and writing of key-value data, with JSON built in. Read files through one API whatever
the format, build data to write without caring about the output format, and define **codecs** that map your C# types
to and from data in a single declaration.

```bash
dotnet add package OliverHesse.DataCodec
```

## Reading data

```csharp
using DataCodec.Api.Deserialization;
using DataCodec.Json.Deserialization;

IDataDecoder decoder = JsonDataDecoder.Default;
IDataObject root = decoder.DecodeFile("player.json").GetOrThrow();

int id = root.GetInt("id").GetOrThrow();
IDataObject[] tags = root.AsArray("tags").GetOrThrow();
```

Getters return a `DataResult<T>`: either the value, or the error with its location, e.g.
`$.tags[1]: expected Number but was String`.

## Writing data

```csharp
using DataCodec.Api.Serialization;
using DataCodec.Json.Serialization;

var root = new SerializationObject();
root.WriteInt("id", 1);
root.WriteObject("stats").WriteDouble("speed", 1.5);

var tags = root.WriteArray("tags");
tags.AppendInt(12);
tags.AppendInt(23);

root.EncodeToFile(JsonDataEncoder.Indented, "player.json");
```

## Codecs

A codec describes how a type maps to data once, and is used for both reading and writing:

```csharp
using DataCodec.Codecs;
using DataCodec.Json.Deserialization;
using DataCodec.Json.Serialization;

enum Color { Red, Green, Blue }

record Player(int Id, string Name, int Lives, List<Color> Colors);

static readonly ICodec<Player> PlayerCodec = Codec.Object(
    Codec.Int.Field("id", (Player p) => p.Id),
    Codec.String.Validate(s => s.Length > 0, "must not be empty").Field("name", (Player p) => p.Name),
    Codec.Int.Range(0, 99).Optional("lives", 3, (Player p) => p.Lives),
    Codec.Enum<Color>().ListOf().Optional("colors", () => [], (Player p) => p.Colors),
    (id, name, lives, colors) => new Player(id, name, lives, colors));

DataResult<Player> loaded = PlayerCodec.DecodeFile(JsonDataDecoder.Default, "player.json");
PlayerCodec.EncodeToFile(new Player(1, "Ada", 3, [Color.Red]), JsonDataEncoder.Indented, "player.json");
```

Built-in codecs include primitives, `ListOf`, `DictionaryOf`, `Codec.Enum<T>()`, `Xmap` for conversions, and
`Validate`, `Min`, `Max`, `Range` and `Clamp` for rules.

## Errors

- **Decoding** never throws for bad data: it returns a failed `DataResult` whose error names where it happened.
  Call `GetOrThrow()` if you would rather have an exception.
- **Encoding** to a stream or file throws `DataEncodeException` (with the path) when a value can't be written, and
  I/O errors throw as usual. `EncodeToString` returns a `DataResult` instead.

## License

MIT, see [LICENSE](LICENSE).
