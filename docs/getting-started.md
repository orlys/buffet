# 開始使用

## .NET

需要 [.NET 10 SDK]。

```shell
dotnet run --project src/Buffet
```

網址會印在主控台。資料庫預設是工作目錄下的 `buffet.db`。

## Docker

Buffet 沒有發佈容器映像，要自己建：

```shell
docker build -t buffet .
docker run -p 5000:8080 -v ./buffet-data:/data buffet
```

然後打開 `http://localhost:5000/`。資料庫在 volume 裡的 `db/buffet.db`。

沒有 Docker 也可以用 SDK 直接建容器映像（`dotnet publish -t:PublishContainer`），做法見 [AGENTS.md](../AGENTS.md) 的〈部署〉。

## 備份

最簡單的做法是先停止服務，再把 `buffet.db` 複製走；如果旁邊還有 `buffet.db-wal`、`buffet.db-shm`，也要一起複製。不想停機的話，用 `sqlite3 buffet.db ".backup 'backup.db'"`。

## 建置與測試

```shell
dotnet build Buffet.slnx
dotnet test Buffet.slnx
```

[.NET 10 SDK]: https://dotnet.microsoft.com/download/dotnet/10.0
