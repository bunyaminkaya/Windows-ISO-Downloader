# Windows ISO Downloader: EXE oluşturma ve GitHub'da yayımlama

## Windows bilgisayarda EXE oluştur

1. Bu ZIP'i çıkar. `WindowsISODownloader` klasöründeki `Kurulum.cmd` dosyasını çalıştır. SDK kurulu değilse kurulum betiği .NET 8 SDK'yı `%LOCALAPPDATA%\WID\dotnet` altına indirir ve kısa yolda derler.
2. Derleme bittiğinde EXE şuradadır: `WindowsISODownloader\bin\App\Windows ISO Downloader.exe`.
3. Uygulama dosyasını **tek başına** dağıtma. Aynı klasördeki `Fido.ps1` ve `FIDO-LICENSE.txt` dosyalarıyla birlikte bir ZIP oluştur. Windows ISO bağlantı çözümlemesi için Fido gereklidir.
4. Üç dosyanın bulunduğu klasörden programı açıp ürünü/sürümü seçerek Windows üzerinde dene. Bir açılış hatası olursa `%LOCALAPPDATA%\WID\hata.log` ve `%LOCALAPPDATA%\WID\kurulum.log` dosyalarına bak.

.NET 8 SDK zaten kuruluysa PowerShell'de proje klasöründen doğrudan derleme komutu:

```powershell
dotnet publish .\WindowsISODownloader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\bin\ReleasePackage
Copy-Item -Path .\Fido.ps1,.\FIDO-LICENSE.txt -Destination .\bin\ReleasePackage\
```

ARM64 Windows EXE için `-r win-arm64` kullan.

## GitHub Actions ile otomatik derleme

1. GitHub'da yeni bir depo oluştur. Bu paketin **içindekileri** deponun köküne yükle: `.github/workflows/build-windows.yml`, `WindowsISODownloader/` ve bu rehber. `.github` klasörünü ZIP içinde veya yalnızca `WindowsISODownloader` altında bırakma. Web arayüzü gizli klasörü yüklemeyi zorlaştırırsa Git/GitHub Desktop ile depoya ekle.
2. GitHub'da **Actions → Build Windows EXE → Run workflow** seç. Windows runner .NET 8 ile `win-x64` tek EXE'yi derler.
3. Çalışma başarılı olunca sayfanın altındaki **Artifacts → Windows-ISO-Downloader-win-x64** öğesini indir. İndirilen Actions artifact ZIP'inin içinde `Windows-ISO-Downloader-win-x64.zip` vardır; ikinci ZIP, kullanıcıların indireceği EXE + Fido paketidir.
4. Herkesin indirebilmesi için **Releases → Draft a new release** ekranında bir sürüm etiketi (ör. `v3.0`) oluşturup bu ikinci ZIP'i ekleyerek yayımla. Yalnız `v*` etiketi depoya gönderirsen iş akışı aynı ZIP'i otomatik Release olarak da yayımlar; iki yöntemi aynı etiket için birlikte kullanma.

Git komut satırını kullanıyorsan, Actions'ın otomatik Release oluşturması için:

```bash
git tag v3.0
git push origin v3.0
```

Etiket gönderilmeden önce kaynak kodun deponun varsayılan dalında olmalıdır. Yayımlanan ZIP'teki EXE kendine yeterli .NET çalışma zamanı içerir; son kullanıcı SDK kurmaz. Bu sürümün Windows x64 EXE'si .NET 8.0.425 ile derlendi ve canlı yayın adresleri denetlendi. Windows arayüzünün gerçek açılışı ve büyük dosyaların eksiksiz indirilmesi bu Linux çalışma ortamında denenemedi; kapsam için `DOGRULAMA-RAPORU.md` dosyasına bakın.
