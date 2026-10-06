$ErrorActionPreference = "Stop"
$apiUrl = "http://localhost:5000/api"

Write-Host "1. Registering new user..."
$email = "concurrent_$(Get-Random)@example.com"
$registerBody = @{
    email = $email
    password = "TestPassword123!"
    confirmPassword = "TestPassword123!"
    fullName = "Test User"
    phoneNumber = "09$(Get-Random -Minimum 10000000 -Maximum 99999999)"
    cccd = "123456789012"
} | ConvertTo-Json

try {
    $regResp = Invoke-WebRequest -UseBasicParsing -Uri "$apiUrl/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    $userId = ($regResp.Content | ConvertFrom-Json).UserId
    Write-Host "   -> User created: $userId"
} catch {
    Write-Host "Register Failed: $($_.Exception.Response.StatusCode)"
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    Write-Host $reader.ReadToEnd()
    exit
}

Write-Host "2. Logging in to get Cookie..."
$loginBody = @{
    email = $email
    password = "TestPassword123!"
} | ConvertTo-Json

$loginResp = Invoke-WebRequest -UseBasicParsing -Uri "$apiUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -SessionVariable session
$cookie = $loginResp.Headers["Set-Cookie"].Split(';')[0]
Write-Host "   -> Cookie acquired: $($cookie.Substring(0, 20))..."

Write-Host "3. Depositing 100,000 VND via Webhook..."
$secret = "my_super_secret_webhook_key"
$txId = "tx-$(Get-Random)"
$timestamp = [int][double]::Parse((Get-Date (Get-Date).ToUniversalTime() -UFormat %s))
$payload = "{`"transactionId`":`"$txId`",`"referenceId`":`"$userId`",`"amount`":100000,`"status`":`"SUCCESS`",`"timestamp`":$timestamp}"

$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($secret))
$hashBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload))
$signature = [BitConverter]::ToString($hashBytes) -replace '-', ''
$signature = $signature.ToLower()

Invoke-WebRequest -UseBasicParsing -Uri "$apiUrl/webhooks/deposit" -Method Post -Body $payload -ContentType "application/json" -Headers @{ "X-Signature" = $signature } | Out-Null
Write-Host "   -> Deposit successful."

Write-Host "4. Firing 2 concurrent withdrawal requests (80,000 VND each)..."
# Using C# internally for exact concurrent HTTP task firing
$code = @"
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class ConcurrencyTester {
    public static async Task<string[]> RunConcurrent(string url, string cookieStr) {
        var handler = new HttpClientHandler();
        handler.CookieContainer = new System.Net.CookieContainer();
        var rawToken = cookieStr.Replace("jwt=", "").Split(';')[0];
        handler.CookieContainer.Add(new Uri(url), new System.Net.Cookie("jwt", rawToken));

        using (var client = new HttpClient(handler)) {
            var content1 = new StringContent("{\"amount\": 80000}", Encoding.UTF8, "application/json");
            var content2 = new StringContent("{\"amount\": 80000}", Encoding.UTF8, "application/json");
            
            var t1 = client.PostAsync(url, content1);
            var t2 = client.PostAsync(url, content2);
            
            var r1 = await t1;
            var r2 = await t2;
            
            return new string[] { "Status: " + r1.StatusCode + " | Content: " + await r1.Content.ReadAsStringAsync(), 
                                  "Status: " + r2.StatusCode + " | Content: " + await r2.Content.ReadAsStringAsync() };
        }
    }
}
"@
Add-Type -TypeDefinition $code -Language CSharp -ReferencedAssemblies "System.Net.Http"
$results = [ConcurrencyTester]::RunConcurrent("$apiUrl/wallet/withdraw", $cookie).GetAwaiter().GetResult()

Write-Host "   -> Thread 1 Response: $($results[0])"
Write-Host "   -> Thread 2 Response: $($results[1])"

Write-Host "5. Checking Final Balance..."
$balResp = Invoke-WebRequest -UseBasicParsing -Uri "$apiUrl/wallet/balance" -Method Get -WebSession $session
$bal = $balResp.Content | ConvertFrom-Json
Write-Host "   -> Final Available Balance: $($bal.availableBalance) VND"
Write-Host "   -> Final Hold Amount: $($bal.holdAmount) VND"
