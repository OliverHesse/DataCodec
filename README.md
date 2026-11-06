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

Built-in codecs include primitives, `ListOf`, `ArrayOf`, `DictionaryOf`, `Codec.Enum<T>()`, `Xmap` for conversions,
`Upcast` and `Codec.Recursive` for type hierarchies and recursive data, and `Validate`, `Min`, `Max`, `Range` and
`Clamp` for rules.

## Polymorphic types

To read and write an interface with several implementations, build each implementation's codec with
`Codec.For<TBase>()`. The getters take the real type, so there is nothing to cast, and the result is a codec for the
base type:

```csharp
interface IShape;
record Circle(double R) : IShape;
record Square(double S) : IShape;

static readonly IMapCodec<IShape> CircleCodec = Codec.For<IShape>().Object(
    Codec.Double.Field("r", (Circle c) => c.R),
    r => new Circle(r));
```

Encoding an `IShape` that is not a `Circle` with `CircleCodec` throws `DataEncodeException`. An existing codec can be
used for the base type with `Codec.For<IShape>().From(codec)`.

## Type dispatch

`Codec.Dispatch` reads and writes any implementation as one flat object whose `type` key picks the codec for the
rest of it:

```csharp
static readonly Dictionary<string, IMapCodec<IShape>> ShapeTypes = new()
{
    ["circle"] = CircleCodec,
    ["square"] = Codec.For<IShape>().Object(Codec.Double.Field("s", (Square s) => s.S), s => new Square(s)),
};

static readonly ICodec<IShape> ShapeCodec = Codec.Dispatch<IShape, string>(
    Codec.String,
    shape => shape switch { Circle => "circle", Square => "square", _ => "unknown" },
    ShapeTypes.GetValueOrDefault);

ShapeCodec.EncodeToString(new Circle(2), JsonDataEncoder.Compact);   // {"type":"circle","r":2}
```

The lookup runs on every call, so types can be added after the codec is created. The type key can be any codec,
such as an enum, and its name can be changed with the `typeKey` argument.

## Recursive codecs

`Codec.Recursive` passes the codec being built to its own definition:

```csharp
record Node(string Name, Node[] Children);

static readonly ICodec<Node> NodeCodec = Codec.Recursive<Node>(self => Codec.Object(
    Codec.String.Field("name", (Node n) => n.Name),
    self.ArrayOf().Optional("children", () => [], (Node n) => n.Children),
    (name, children) => new Node(name, children)));
```

## Errors

- **Decoding** never throws for bad data: it returns a failed `DataResult` whose error names where it happened.
  Call `GetOrThrow()` if you would rather have an exception.
- **Encoding** to a stream or file throws `DataEncodeException` (with the path) when a value can't be written, and
  I/O errors throw as usual. `EncodeToString` returns a `DataResult` instead.
- **Custom codecs** can report errors the same way: build paths with `DataPath` (`DataPath.Key`, `DataPath.Index`),
  fail with `DataResult<T>.Fail(path, reason)`, and move a child's encode error under its parent with
  `DataEncodeException.Under`.

## Changes in 0.2.0

- `Codec.Object` now returns `IMapCodec<T>`, which extends `ICodec<T>`. Source code compiles unchanged, but code
  compiled against 0.1.0 must be recompiled.
- New: `Codec.For`, `Codec.Dispatch`, `Codec.Recursive`, `ArrayOf`, `Upcast`, public `DataPath` and
  `DataResult<T>.Fail(path, reason)`.

## License

MIT, see [LICENSE](LICENSE).
