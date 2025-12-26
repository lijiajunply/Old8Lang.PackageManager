# 包签名验证系统

Old8Lang Package Manager 包签名验证系统提供了完整的包安全保障机制,使用 RSA 数字签名技术确保包的真实性和完整性。

## 功能特性

### 核心功能
- ✅ **RSA 数字签名**: 使用 RSA-2048 算法对包进行签名
- ✅ **多哈希算法支持**: 支持 SHA256 和 SHA512 哈希算法
- ✅ **证书管理**: 生成、导入、导出和管理 X.509 证书
- ✅ **信任列表**: 管理可信任的签名证书列表
- ✅ **签名验证**: 验证包签名的有效性和完整性
- ✅ **过期检测**: 自动检测过期的证书
- ✅ **证书链验证**: 可选的证书链完整性验证

### 安全特性
- 包哈希值验证,防止文件篡改
- 公钥/私钥分离,私钥安全存储
- 签名时间戳记录
- 签名者身份信息记录
- 可选的强制签名验证模式

## 架构设计

### 核心组件

```
┌─────────────────────────────────────────────────────────┐
│              Package Signature System                    │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  ┌───────────────────┐      ┌──────────────────────┐   │
│  │ PackageSignature  │      │ SignatureVerification│   │
│  │ Service           │─────▶│ Result               │   │
│  └───────────────────┘      └──────────────────────┘   │
│           │                                              │
│           ▼                                              │
│  ┌───────────────────┐      ┌──────────────────────┐   │
│  │ Certificate       │      │ Certificate          │   │
│  │ Store             │─────▶│ Info                 │   │
│  └───────────────────┘      └──────────────────────┘   │
│           │                                              │
│           ▼                                              │
│  ┌───────────────────┐                                  │
│  │ Certificate       │                                  │
│  │ Management        │                                  │
│  └───────────────────┘                                  │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### 签名文件格式

签名文件 (`.sig`) 采用 JSON 格式:

```json
{
  "Algorithm": "RSA-SHA256",
  "SignatureData": "base64-encoded-signature",
  "Timestamp": "2024-12-26T10:30:00Z",
  "PackageHash": "base64-encoded-hash",
  "HashAlgorithm": "SHA256",
  "Signer": {
    "CertificateThumbprint": "ABC123...",
    "PublicKey": "-----BEGIN CERTIFICATE-----\n...",
    "Name": "Package Publisher",
    "Email": "publisher@example.com",
    "NotBefore": "2024-01-01T00:00:00Z",
    "NotAfter": "2029-01-01T00:00:00Z"
  },
  "Version": "1.0"
}
```

## 配置

### appsettings.json

```json
{
  "Security": {
    "EnablePackageSigning": false,
    "RequireTrustedCertificates": false,
    "ValidateCertificateChain": false,
    "EnableChecksumValidation": true,
    "AllowedHashAlgorithms": ["SHA256", "SHA512"]
  }
}
```

#### 配置选项说明

- **EnablePackageSigning**: 是否启用包签名验证 (默认: `false`)
- **RequireTrustedCertificates**: 是否要求证书必须在信任列表中 (默认: `false`)
- **ValidateCertificateChain**: 是否验证证书链 (默认: `false`)
- **EnableChecksumValidation**: 是否启用校验和验证 (默认: `true`)
- **AllowedHashAlgorithms**: 允许的哈希算法列表

## CLI 命令使用

### 1. 生成证书

```bash
# 生成自签名证书
o8pm cert generate "My Package Signer" --email "signer@example.com" --years 5 --output mycert.pfx

# 查看证书信息
o8pm cert info mycert.pfx
```

**输出示例:**
```
Certificate generated successfully!
Output: mycert.pfx
Thumbprint: A1B2C3D4E5F6...
Valid until: 2029-12-26
```

### 2. 签名包

```bash
# 使用指定证书签名
o8pm sign mypackage.o8pkg --cert mycert.pfx

# 使用密码保护的证书
o8pm sign mypackage.o8pkg --cert mycert.pfx --cert-password "mypassword"

# 自动生成证书并签名
o8pm sign mypackage.o8pkg
```

**输出示例:**
```
Package signed successfully!
Signature file: mypackage.o8pkg.sig
Signed by: My Package Signer
Thumbprint: A1B2C3D4E5F6...
```

### 3. 验证签名

```bash
# 验证包签名
o8pm verify mypackage.o8pkg
```

**输出示例 (成功):**
```
✓ Signature is valid
Signed by: My Package Signer
Signed at: 2024-12-26 10:30:00
Certificate: A1B2C3D4E5F6...
```

**输出示例 (失败):**
```
✗ Signature verification failed! Package may have been tampered with.
```

### 4. 证书管理

```bash
# 导出证书 (仅公钥)
o8pm cert export mycert.pfx mycert.cer

# 查看证书详情
o8pm cert info mycert.cer
```

## API 使用

### REST API 端点

#### 1. 验证包签名

```http
GET /api/v3/signatures/verify/{packageId}/{version}
```

**响应示例:**
```json
{
  "isValid": true,
  "message": "签名验证成功",
  "signature": {
    "algorithm": "RSA-SHA256",
    "timestamp": "2024-12-26T10:30:00Z",
    "signer": {
      "name": "Package Publisher",
      "email": "publisher@example.com",
      "certificateThumbprint": "ABC123..."
    }
  },
  "isTrusted": true,
  "verifiedAt": "2024-12-26T11:00:00Z"
}
```

#### 2. 签名包

```http
POST /api/v3/signatures/sign/{packageId}/{version}
Authorization: Bearer <token>
Content-Type: application/json

{
  "certificateThumbprint": "ABC123...",
  "certificatePassword": "optional-password"
}
```

#### 3. 获取信任证书列表

```http
GET /api/v3/signatures/certificates/trusted
```

#### 4. 添加信任证书

```http
POST /api/v3/signatures/certificates/trusted
Authorization: Bearer <admin-token>
Content-Type: multipart/form-data

certificateFile: <file>
password: <optional>
```

#### 5. 生成证书

```http
POST /api/v3/signatures/certificates/generate
Authorization: Bearer <token>
Content-Type: application/json

{
  "subjectName": "My Package Signer",
  "email": "signer@example.com",
  "validityYears": 5
}
```

#### 6. 导出证书

```http
GET /api/v3/signatures/certificates/export/{thumbprint}?includePrivateKey=false
Authorization: Bearer <token>
```

## 编程使用

### .NET 代码示例

#### 签名包

```csharp
using Old8Lang.PackageManager.Server.Services;
using System.Security.Cryptography.X509Certificates;

// 加载证书
var certificate = new X509Certificate2("mycert.pfx", "password");

// 签名服务
var signatureService = serviceProvider.GetRequiredService<IPackageSignatureService>();

// 签名包
var signature = await signatureService.SignPackageAsync(
    packagePath: "mypackage.o8pkg",
    certificate: certificate
);

Console.WriteLine($"Package signed by: {signature.Signer.Name}");
```

#### 验证签名

```csharp
// 验证包签名
var result = await signatureService.VerifyPackageSignatureAsync("mypackage.o8pkg");

if (result.IsValid)
{
    Console.WriteLine($"✓ Valid signature from {result.Signature?.Signer.Name}");
    Console.WriteLine($"Trusted: {result.IsTrusted}");
}
else
{
    Console.WriteLine($"✗ Invalid: {result.Message}");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

#### 管理证书

```csharp
var certificateStore = serviceProvider.GetRequiredService<ICertificateStore>();

// 添加信任证书
await certificateStore.AddTrustedCertificateAsync(certificate);

// 获取信任列表
var trustedCerts = await certificateStore.GetTrustedCertificatesAsync();

// 生成自签名证书
var newCert = await certificateStore.GenerateSelfSignedCertificateAsync(
    subjectName: "Test Signer",
    validityYears: 5
);
```

## 安全最佳实践

### 1. 证书管理
- ✅ 使用强密码保护私钥文件 (.pfx)
- ✅ 妥善保管私钥,不要提交到版本控制系统
- ✅ 定期轮换证书 (建议每 1-2 年)
- ✅ 在生产环境使用受信任的 CA 签发的证书

### 2. 签名策略
- ✅ 对所有发布的包进行签名
- ✅ 在 CI/CD 流程中自动化签名过程
- ✅ 使用环境变量存储证书密码
- ✅ 记录所有签名操作的审计日志

### 3. 验证策略
- ✅ 在生产环境启用 `EnablePackageSigning`
- ✅ 启用 `RequireTrustedCertificates` 强制信任验证
- ✅ 定期审查信任证书列表
- ✅ 监控签名验证失败事件

### 4. 部署建议

```json
// 开发环境
{
  "Security": {
    "EnablePackageSigning": false,
    "RequireTrustedCertificates": false
  }
}

// 生产环境
{
  "Security": {
    "EnablePackageSigning": true,
    "RequireTrustedCertificates": true,
    "ValidateCertificateChain": true
  }
}
```

## 故障排除

### 常见问题

#### 1. "Certificate does not contain a private key"
**原因**: 证书文件不包含私钥
**解决**: 确保使用 `.pfx` 格式的证书文件,而不是 `.cer` 格式

#### 2. "Package hash mismatch"
**原因**: 包文件在签名后被修改
**解决**: 重新签名包,或检查包文件完整性

#### 3. "Certificate expired"
**原因**: 签名证书已过期
**解决**: 生成新证书并重新签名包

#### 4. "Certificate not in trusted list"
**原因**: 证书未添加到信任列表
**解决**:
```bash
# API 方式
POST /api/v3/signatures/certificates/trusted

# 或在配置中禁用强制信任
"RequireTrustedCertificates": false
```

## 技术规范

### 签名算法
- **非对称加密**: RSA-2048
- **哈希算法**: SHA256 / SHA512
- **填充模式**: PKCS#1 v1.5
- **证书格式**: X.509 v3
- **签名格式**: Base64 编码

### 文件存储结构

```
packages/
├── certificates/              # 证书存储目录
│   ├── ABC123....cer         # 公钥证书
│   ├── ABC123....json        # 证书元数据
│   └── ...
├── private-keys/             # 私钥存储目录 (需要严格权限控制)
│   ├── ABC123....pfx
│   └── ...
└── {packageId}/
    └── {version}/
        ├── package.o8pkg     # 包文件
        └── package.o8pkg.sig # 签名文件
```

### 性能考虑

- **签名操作**: ~100-200ms (RSA-2048)
- **验证操作**: ~50-100ms
- **证书生成**: ~200-300ms
- **哈希计算**: O(n) 线性时间复杂度

## 扩展与集成

### CI/CD 集成示例

#### GitHub Actions

```yaml
name: Sign Package

on:
  release:
    types: [created]

jobs:
  sign:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Build Package
        run: dotnet pack -c Release

      - name: Sign Package
        env:
          CERT_PASSWORD: ${{ secrets.CERT_PASSWORD }}
        run: |
          o8pm sign ./bin/Release/MyPackage.1.0.0.o8pkg \
            --cert signing-cert.pfx \
            --cert-password "$CERT_PASSWORD"

      - name: Upload to Registry
        run: |
          curl -X POST https://registry.example.com/api/v3/packages \
            -H "Authorization: Bearer ${{ secrets.API_TOKEN }}" \
            -F "package=@./bin/Release/MyPackage.1.0.0.o8pkg" \
            -F "signature=@./bin/Release/MyPackage.1.0.0.o8pkg.sig"
```

## 更新日志

### v1.0.0 (2024-12-26)
- ✅ 初始发布
- ✅ RSA-SHA256/SHA512 签名支持
- ✅ X.509 证书管理
- ✅ 信任列表管理
- ✅ CLI 命令支持
- ✅ REST API 支持
- ✅ 自动签名验证集成

## 参考资料

- [X.509 Certificate Standard](https://www.itu.int/rec/T-REC-X.509)
- [RSA Cryptography Specifications](https://www.rfc-editor.org/rfc/rfc8017)
- [.NET X509Certificate2 Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)
