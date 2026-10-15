# Windows ISO Downloader 3.6.0

Bünyamin Kaya tarafından geliştirilen bağımsız Windows indirme aracı. Microsoft ürünü değildir.

Windows ISO ve desteklenen Microsoft kurulum dosyaları için ürün, sürüm, mimari ve dil seçimi sunar. x64 ve ARM64 uygulama paketleri bulunur.

- [Son sürüm ve paketler](https://github.com/bunyaminkaya/Windows-ISO-Downloader/releases/latest)
- [Tanıtım ve kullanım](https://bunyaminkaya.github.io/Windows-ISO-Downloader/)
- [Resmî indirme sayfası](https://bunyaminkaya.com/windows-iso-downloader/)
- [Sürüm notları](SURUM-3.6.0.md)
- [Komut satırı kılavuzu](CLI-KILAVUZ.md)

ZIP paketinin tamamını çıkarın; EXE, Fido.ps1 ve FIDO-LICENSE.txt birlikte kalmalıdır. Paketler self-contained'dır; çalıştırmak için .NET SDK kurulumu gerekmez. EXE dosyaları henüz Authenticode ile imzalanmamıştır. SHA256SUMS.txt bütünlük kontrolü içindir, yayıncı imzasının yerine geçmez.

Kaynak derleme: `dotnet publish WindowsISODownloader/WindowsISODownloader.csproj -c Release -r win-x64 --self-contained true` (ARM64 için `win-arm64`).

İndirilen değerlendirme ürünlerinin süre ve lisans koşulları geçerlidir. Dosya indirmek kullanım veya etkinleştirme hakkı vermez. Fido ayrı bir GPL-3.0-or-later bileşenidir; lisans metni dağıtıma dahildir.
