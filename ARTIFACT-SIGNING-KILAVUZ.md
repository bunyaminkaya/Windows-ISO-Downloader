# Windows ISO Downloader: Microsoft Artifact Signing ile imzalama

Bu paket henüz imzasızdır. `ARTIFACT-SIGNING-IMZALA.ps1` betiği, kullanıcının kendi Windows bilgisayarında Artifact Signing hesabıyla giriş yapmasından sonra EXE'yi imzalar, doğrular ve **yeni bir imzalı ZIP** üretir. Betik başarısız olursa imzalı ZIP üretmez. Azure parola, erişim anahtarı veya sertifika özel anahtarını bu dosyalara yazmayın.

Gerekenler: kimlik doğrulaması **Completed**, etkin **Public Trust** sertifika profili, oturum açan kullanıcıya **Artifact Signing Certificate Profile Signer** rolü, Windows SDK'nın güncel x64 SignTool'u (en az 10.0.2261.755), .NET 8 Runtime, Microsoft Artifact Signing Client Tools (Dlib), Azure CLI. Azure CLI için `az login` çalıştırın. Client Tools kurulumu: `winget install -e --id Microsoft.Azure.ArtifactSigningClientTools`. Windows SDK ve gerekirse .NET 8 Runtime'ı Microsoft'un belgelerinden yükleyin.

ZIP'in tamamını çıkarıp PowerShell'de betiğin olduğu klasöre geçin. Azure portalında Artifact Signing hesabının **Account URI**, hesap adı ve **Certificate profiles** altındaki profil adını bulun. SDK ve Client Tools içindeki x64 dosya yollarını kendi sisteminize göre girin:

```powershell
az login
.\ARTIFACT-SIGNING-IMZALA.ps1 `
  -Endpoint 'https://weu.codesigning.azure.net' `
  -AccountName 'HESAP_ADI' `
  -CertificateProfileName 'PUBLIC_TRUST_PROFIL_ADI' `
  -SignToolPath 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe' `
  -DlibPath 'C:\Program Files (x86)\Microsoft\ArtifactSigningClientTools\bin\Azure.CodeSigning.Dlib.dll'
```

Endpoint örnektir; Azure portalındaki kendi bölgenizin URI'sini kullanın. SignTool ve Dlib yolları kurulu sürüme göre değişebilir. Başarılı işlem sonunda `Windows-ISO-Downloader-win-x64-IMZALI.zip` dosyasını ve imzalı EXE SHA-256 değerini verir. ZIP içindeki EXE'de Dosya Özellikleri > Dijital İmzalar alanını denetleyin. ZIP'i imzalamadan önce EXE imzalanır; imzalandıktan sonra EXE'yi değiştirmeyin. İmzalı yeni uygulamada SmartScreen itibar uyarısı yine görülebilir; imza “Bilinmeyen yayımlayıcı” yerine doğrulanmış yayımlayıcı kimliğini sağlar.

Kaynak: https://learn.microsoft.com/en-us/azure/artifact-signing/how-to-signing-integrations ve https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation .
