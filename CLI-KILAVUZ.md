# Komut satırı kullanımı — 3.6.0
ZIP’in tamamını çıkarın. Fido.ps1 ve lisansı EXE ile aynı klasörde kalsın.
`wid.cmd` komutunu kullanın; Windows GUI alt sistemi EXE’sinin bitmesini batch dosyası bekler. Doğrudan EXE’yi CMD’den çağırıyorsanız `start /wait "" "Windows ISO Downloader.exe" ...` kullanın.

```bat
wid.cmd --help
wid.cmd --product win11 --lang Turkish --arch x64 --out "D:\ISO" --json
wid.cmd --product win11 --arch arm64 --list --json
wid.cmd --product server --release 2025VHDX --lang enus --arch x64 --out "D:\VM" --json
wid.cmd --product office --arch arm64 --channel MonthlyEnterprise --languages tr-tr,en-us --exclude Access,Publisher --visio --project --out "D:\Office" --json
wid.cmd --product dotnet-sdk --release 10.0 --arch arm64 --url-only --json
wid.cmd --product terminal --arch arm64 --out "D:\Tools" --json
```

Ürün kodları: win11, win10, server, ltsc, posready, office, office2019, office2016, edge, powertoys, vscode, sysinternals, vcredist, terminal, adk, winpe, sql, vs-community, vs-buildtools, dotnet-sdk, dotnet-runtime, dotnet-desktop, dotnet-aspnet.

`--list` sürüm kimliklerini, `--url-only` bağlantı ve yayıncı hash bilgisini döndürür. Bağlantılar süreli olabilir. `--release` değeri listede yer alan kimlik olmalıdır. Windows için Fido kataloğundaki tam ad veya tek bir kaydı belirleyen sürüm öneki kullanılabilir. Mevcut katalogda 24H2 yoktur.

`--out` kayıt klasörüdür. Var olan tamamlanmış dosyaların üstüne yazmak için açıkça `--force` gerekir. Aynı seçim ve klasörle yeniden çağırmak uygun ETag/Range varsa .part dosyasından devam eder. CLI kendiliğinden kurulum başlatmaz; Office dosyalarını indirmek için imzası doğrulanan Microsoft ODT çalıştırılır, kurulum için Office-Kur.cmd ayrıca sizin seçiminizle açılır.

`--expected-sha256` güvenilir kaynaktan aldığınız 64 karakterli değerdir. Yayıncının sağladığı hash ile çelişirse işlem reddedilir. `--ui-lang tr|en` hata metni dilini belirler.

JSON modunda tek sonuç nesnesi stdout’a yazılır. Çıkış kodları: 0 başarılı, 1 hata, 2 iptal. İndirme ilerlemesi JSON çıktısını kirletmez. Ctrl+C konsolun ilettiği iptal olayını kullanır. Windows CLI çalışma testleri CI’ye eklendi; bu Linux ortamında Windows EXE çalıştırılmadı.
