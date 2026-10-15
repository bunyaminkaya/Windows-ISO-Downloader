# SmartScreen uyarısı ve yayımlayıcı doğrulaması

Ekrandaki “Windows kişisel bilgisayarınızı korudu” ve “Bilinmeyen yayımlayıcı” uyarısı, bu EXE'nin güvenilir bir yayımlayıcı sertifikasıyla imzalanmamış olmasından kaynaklanır. SmartScreen ayrıca dosyanın indirme/çalıştırma itibarını değerlendirir. Bu ileti tek başına zararlı yazılım tespiti değildir; yazılımın güvenli olduğunun kanıtı da değildir.

**Bu paket imzasızdır.** EXE'nin adını değiştirmek, sürüm bilgisine Bünyamin Kaya yazmak, SHA-256 yayımlamak veya kendi kendine imzalanmış sertifika kullanmak Windows'taki “Bilinmeyen yayımlayıcı” sorununu güvenilir biçimde çözmez. Kullanıcıya güvenlik korumasını kapatmasını tavsiye etmeyin.

## Yayımlamadan önce yapılacak işlem

1. Bünyamin Kaya adına güvenilir bir sertifika yetkilisinden kod imzalama sertifikası edinin veya Microsoft Artifact Signing için kimlik doğrulamasını tamamlayın. Sertifika ve özel anahtarı projeye veya ZIP'e koymayın.
2. Windows SDK SignTool kurulu bir Windows makinede nihai EXE'yi sertifikanızla imzalayın. Sertifika Windows sertifika deposundaysa, bu klasörde PowerShell'de örnek: `./IMZALAMA.ps1 -CertificateThumbprint SERTIFIKANIN40HANELISHA1KIMLIGI -ExePath './Windows ISO Downloader.exe'`. Bu betik SHA-256 imza ve zaman damgası ekler; ardından SignTool ve Authenticode ile yayımlayıcıyı doğrular. Sertifikanız farklı bir imzalama hizmetindeyse sağlayıcının resmî imzalama yöntemini kullanıp `signtool verify /pa /all /tw /v "Windows ISO Downloader.exe"` komutunu çalıştırın.
3. **İmzalama tamamlandıktan sonra** EXE'yi ZIP'e koyun; imzalı dosyayı sonradan değiştirmeyin. Yeni EXE'nin SHA-256 değerini yayımlayın. Windows üzerinde Dosya Özellikleri → Dijital İmzalar kısmında yayımlayıcıyı denetleyin.
4. İmzalı yeni bir uygulamada da SmartScreen “tanınmayan uygulama” uyarısı bir süre görünebilir; geçerli imza yalnızca yayımlayıcı kimliğini doğrular, itibarı anında garanti etmez. Uyarının kesin olarak hiç görünmemesi için Microsoft'un belgelerinde Microsoft Store dağıtımı önerilir.

Kaynak: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation ve https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool .
