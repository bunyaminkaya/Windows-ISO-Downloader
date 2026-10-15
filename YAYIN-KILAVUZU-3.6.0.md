# GitHub yayın adımları
Kaynak ZIP’in kökündeki dosyaları depo köküne yükleyin. .github/workflows klasörü depo kökünde bulunmalıdır. GitHub Actions’ın açık olduğundan emin olun. Haftalık bağlantı işi schedule ile varsayılan dalda çalışır; Actions → Weekly Server link health → Run workflow ile ilk deneme yapılabilir. Job yalnızca ilgili depo için issues:write ve contents:read izinleri kullanır.

Build Windows EXE workflow’u x64 ve ARM64 için test/derleme yapar. v3.6.0 gibi etiketlerde SHA256SUMS.txt ve iki ZIP ile Release hazırlar. Etiket sürümünü csproj Version değeriyle eşleştirin. Windows CLI ve WPF smoke kontrolleri x64 runner’da çalışır; ARM64 derlemesi burada üretilir ama x64 runner’da ARM64 çalışma testi yapılmaz.

Mevcut workflow EXE’yi otomatik imzalamaz. Authenticode imzalama için ARTIFACT-SIGNING-KILAVUZ.md / ARTIFACT-SIGNING-IMZALA.ps1 dosyalarını kullanın. İmzaladıktan sonra ZIP hash’lerini yeniden hesaplayın ve SHA256SUMS.txt’yi güncelleyin. Güncelleme doğrulaması hem mevcut hem yeni EXE’de geçerli ve aynı yayıncıya ait imza ister. İmzasız EXE’ye güvenilir güncelleme rozeti verilmez.

winget, Scoop ve Chocolatey’de otomatik yayın yapılmadı. İmzalama, kararlı sürüm URL’si ve son imzalı ZIP’in hash’i hazır olduğunda ilgili topluluk depolarına paket gönderilebilir. İmzasız ZIP için doğrulanmış yayın iddiası içeren hazır paket yayımlanmaz.
