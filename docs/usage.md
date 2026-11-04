# 使用 feed 與上傳套件

## 使用 feed

```shell
dotnet nuget add source https://<host>/v3/index.json -n buffet
```

之後 `dotnet restore` 就會到這裡找套件。套件頁上有各種用戶端的安裝指令，頁尾的 service index 網址可以直接複製。

## 上傳套件

網頁上沒有上傳頁，請用 NuGet 用戶端推送。service index 是 `https://<host>/v3/index.json`；如果有設 `PathBase`，要接在 host 後面。有設定 API key（`ApiKey` 或 `Authentication:ApiKeys`）時，要帶相符的 key；沒有設定時帶什麼都可以。

.NET CLI（同一個目錄下如果有對應的 `.snupkg`，會一起推送符號套件）：

```shell
dotnet nuget push -s https://<host>/v3/index.json -k <API-KEY> MyPackage.1.0.0.nupkg
```

NuGet CLI：

```shell
nuget push -Source https://<host>/v3/index.json -ApiKey <API-KEY> MyPackage.1.0.0.nupkg
```

Paket：

```shell
paket push --url https://<host> --api-key <API-KEY> MyPackage.1.0.0.nupkg
```

有設定 `Authentication:Credentials` 時，service index 也要帳密，所以要先帶帳密註冊來源，再用來源名稱推送（Linux/macOS 要加 `--store-password-in-clear-text`）：

```shell
dotnet nuget add source https://<host>/v3/index.json -n buffet -u <USER> -p <PASSWORD>
dotnet nuget push -s buffet -k <API-KEY> MyPackage.1.0.0.nupkg
```

新版 NuGet 用戶端拒絕推送到 `http://` 來源。要用 `--allow-insecure-connections` 註冊來源（需要帳密時再加 `-u`/`-p`），再用來源名稱推送：

```shell
dotnet nuget add source http://<host>/v3/index.json -n buffet --allow-insecure-connections
dotnet nuget push -s buffet -k <API-KEY> MyPackage.1.0.0.nupkg
```

回應碼：`201` 表示成功；`409` 表示同 id、同版本已經存在（除非開了 `AllowPackageOverwrites`）；`401` 表示 API key 錯誤或處於唯讀模式；`400` 表示不是有效的套件。
