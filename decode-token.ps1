# JWT Token Decoder
param(
    [string]$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0ZW5hbnRfaWQiOiJhMWIyYzNkNC1lNWY2LTc4OTAtYWJjZC1lZjEyMzQ1Njc4OTAiLCJ1c2VyX2lkIjoiMDAwMDAwMDAtMDAwMC0wMDAwLTAwMDAtMDAwMDAwMDAwMDAxIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiIwMDAwMDAwMC0wMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEiLCJleHAiOjE3NzAyMjkwOTIsImlzcyI6IkVycENsb3VkIiwiYXVkIjoiZXJwLWNsb3VkIn0.HsTF5NLqShg54zhtFGpW-FqblRAEgDP-jK4MvDMGX0w"
)

$parts = $token.Split('.')
$payload = $parts[1]

# Add padding if needed
switch ($payload.Length % 4) {
    2 { $payload += '==' }
    3 { $payload += '=' }
}

# Base64 decode
$bytes = [Convert]::FromBase64String($payload.Replace('-', '+').Replace('_', '/'))
$json = [System.Text.Encoding]::UTF8.GetString($bytes)

Write-Host "JWT Payload:" -ForegroundColor Cyan
Write-Host $json | ConvertFrom-Json | ConvertTo-Json
