<#
.SYNOPSIS
    DMS Integration Test & Demo Script (Windows/PowerShell)
.DESCRIPTION
    Interactively tests the Document Management System.
    - Uses TEMP FILES for JSON payloads to prevent curl argument parsing errors.
    - Generates Rich PDF content.
    - Fixes variable assignment bugs.
#>

# --- Configuration ---
$BaseUrl = "http://localhost:8081/api"
$DocEndpoint = "$BaseUrl/document"
$File1 = "demo_specs_v4.pdf"
$File2 = "demo_invoice_v4.pdf"

# --- Helper Functions ---
function Log-Info ($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Log-Success ($msg) { Write-Host "[SUCCESS] $msg" -ForegroundColor Green }
function Log-Response ($obj) { 
    Write-Host "Server Response:" -ForegroundColor DarkGray
    if ($obj) {
        $json = $obj | ConvertTo-Json -Depth 5
        if ($json.Length -gt 1000) { $json = $json.Substring(0, 1000) + "... (truncated)" }
        Write-Host $json -ForegroundColor Gray 
    } else {
        Write-Host "(No Content)" -ForegroundColor Gray
    }
}
function Log-Error ($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }

function Pause-Step {
    Write-Host ""
    Read-Host "Press [ENTER] to continue to the next step..."
    Write-Host "----------------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ""
}

# --- Robust Multi-line PDF Generator ---
function New-SimplePdf {
    param (
        [string]$Filename,
        [string]$TextContent
    )

    # 1. Build Text Stream
    # /F1 10 Tf = Font Helvetica 10pt, 14 TL = Line Spacing
    $Commands = @("BT", "/F1 10 Tf", "14 TL", "50 750 Td")
    
    $Lines = $TextContent -split '\|'
    foreach ($Line in $Lines) {
        # Escape parenthesis and backslashes
        $SafeLine = $Line.Trim() -replace '\\', '\\\\' -replace '\(', '\(' -replace '\)', '\)'
        $Commands += "($SafeLine) Tj T*"
    }
    $Commands += "ET"
    
    $StreamText = $Commands -join " "
    $StreamBytes = [System.Text.Encoding]::ASCII.GetBytes($StreamText)

    # 2. Build PDF Objects
    $Header = "%PDF-1.4"
    $Obj1 = "1 0 obj`n<< /Type /Catalog /Pages 2 0 R >>`nendobj"
    $Obj2 = "2 0 obj`n<< /Type /Pages /Kids [3 0 R] /Count 1 >>`nendobj"
    $Obj3 = "3 0 obj`n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>`nendobj"
    $Obj4 = "4 0 obj`n<< /Length $($StreamBytes.Length) >>`nstream`n$StreamText`nendstream`nendobj"
    
    # 3. Write File
    $Body = "$Header`n$Obj1`n$Obj2`n$Obj3`n$Obj4"
    $Trailer = "`ntrailer`n<< /Size 5 /Root 1 0 R >>`n%%EOF"
    [System.IO.File]::WriteAllText("$PWD\$Filename", "$Body$Trailer", [System.Text.Encoding]::ASCII)
    Log-Info "Generated $Filename"
}

# Check for curl.exe
if (-not (Get-Command "curl.exe" -ErrorAction SilentlyContinue)) {
    Log-Error "curl.exe not found! This script requires curl installed and in your PATH."
    exit 1
}

# --- 1. Setup & File Generation ---
Clear-Host
Log-Info "=== Starting DMS Integration Test (Interactive Demo) ==="
Log-Info "Target API: $BaseUrl"

# Clean up old files
if (Test-Path $File1) { Remove-Item $File1 }
if (Test-Path $File2) { Remove-Item $File2 }

# Generate PDF 1: Technical Specs (Rich Content for Summary)
$SpecsText = "PROJECT DOCUMENTATION: DOCUMENT MANAGEMENT SYSTEM v2.0 | " +
             "1. EXECUTIVE SUMMARY | " +
             "The goal of this project is to build a scalable document archive. " +
             "It leverages a microservices architecture to ensure high availability and decoupling of components. | " +
             " | " +
             "2. ARCHITECTURAL DECISIONS | " +
             "- Messaging: RabbitMQ is used for asynchronous communication between the REST API and Worker services. | " +
             "- Storage: MinIO (S3 compatible) handles large binary object storage (BLOBs). | " +
             "- Database: PostgreSQL is used for structured metadata and relational mapping. | " +
             "- Search: ElasticSearch provides full-text search capabilities over OCR content. | " +
             " | " +
             "3. SECURITY PROTOCOLS | " +
             "All internal communication is secured via private Docker networks. Public access is restricted to the Nginx gateway."
New-SimplePdf -Filename $File1 -TextContent $SpecsText

# Generate PDF 2: Invoice (Rich Content for Summary)
$InvoiceText = "INVOICE #2026-999 | " +
               "Date: January 20, 2026 | " +
               "From: Global Server Solutions Inc. | " +
               "To: Tech Startup Hub Vienna | " +
               "-------------------------------------------------- | " +
               "Description                        Qty      Price | " +
               "-------------------------------------------------- | " +
               "Dell PowerEdge R750 Server          2    $4,500.00 | " +
               "Cisco Catalyst 9200 Switch          5    $1,200.00 | " +
               "Fiber Optic Cabling (100m)          10     $150.00 | " +
               "On-site Installation Service        12     $120.00 | " +
               "-------------------------------------------------- | " +
               "Subtotal: ............................ $17,940.00 | " +
               "Tax (20%): ...........................  $3,588.00 | " +
               "TOTAL DUE: ........................... $21,528.00 | " +
               "-------------------------------------------------- | " +
               "Payment Terms: Net 30. Please include invoice number in transfer."
New-SimplePdf -Filename $File2 -TextContent $InvoiceText

Pause-Step

# --- 2. Upload Documents ---
Log-Info "STEP 1: Uploading Documents"

# Upload Doc 1
Log-Info "Action: POST /api/document/upload (File: $File1)"
$Json1 = curl.exe -s -X POST "$DocEndpoint/upload" `
    -H "accept: application/json" `
    -H "Content-Type: multipart/form-data" `
    -F "file=@$File1;type=application/pdf" `
    -F "tags=specs,tech,architecture"

if ($LASTEXITCODE -ne 0) { Log-Error "Upload failed. Is the API running?"; exit 1 }

$Doc1 = $Json1 | ConvertFrom-Json
$Doc1_ID = $Doc1.id
$Doc1_Name = $Doc1.fileName
Log-Success "Uploaded: $Doc1_Name"
Log-Response $Doc1

# Upload Doc 2
Log-Info "Action: POST /api/document/upload (File: $File2)"
$Json2 = curl.exe -s -X POST "$DocEndpoint/upload" `
    -H "accept: application/json" `
    -H "Content-Type: multipart/form-data" `
    -F "file=@$File2;type=application/pdf" `
    -F "tags=invoice,finance,hardware"

$Doc2 = $Json2 | ConvertFrom-Json
$Doc2_ID = $Doc2.id
$Doc2_Name = $Doc2.fileName # FIX: Capture name for logging later
Log-Success "Uploaded: $Doc2_Name"
Log-Response $Doc2

Pause-Step

# --- 3. Get All Documents ---
Log-Info "STEP 2: Retrieve All Documents"
Log-Info "Action: GET /api/document"

$ListJson = curl.exe -s -X GET "$DocEndpoint" -H "accept: application/json"
$List = $ListJson | ConvertFrom-Json
$Count = $List.Count

Log-Success "Retrieved $Count documents"
Log-Response $List

Pause-Step

# --- 4. Polling for OCR & GenAI ---
Log-Info "STEP 3: Async Background Processing (OCR & GenAI)"
Log-Info "Polling documents for completion..."

$MaxRetries = 30
$Retry = 0
$Doc1_Done = $false
$Doc2_Done = $false

while ($Retry -lt $MaxRetries) {
    $Retry++
    Start-Sleep -Seconds 3
    
    # Check Doc 1
    if (-not $Doc1_Done) {
        $Detail1 = curl.exe -s -X GET "$DocEndpoint/$Doc1_ID" -H "accept: application/json" | ConvertFrom-Json
        if (-not [string]::IsNullOrEmpty($Detail1.metadata.summary)) { $Doc1_Done = $true }
    }

    # Check Doc 2
    if (-not $Doc2_Done) {
        $Detail2 = curl.exe -s -X GET "$DocEndpoint/$Doc2_ID" -H "accept: application/json" | ConvertFrom-Json
        if (-not [string]::IsNullOrEmpty($Detail2.metadata.summary)) { $Doc2_Done = $true }
    }

    $StatusLine = "Attempt $Retry/$MaxRetries : Doc 1 [$(if($Doc1_Done){'OK'}else{'..'})] | Doc 2 [$(if($Doc2_Done){'OK'}else{'..'})]"
    Write-Host -NoNewline "`r$StatusLine"

    if ($Doc1_Done -and $Doc2_Done) {
        Write-Host ""
        Log-Success "Processing Complete!"
        
        Write-Host "`n--- Document 1 ($File1) Results ---" -ForegroundColor Cyan
        Write-Host "OCR Text (excerpt): " -NoNewline; Write-Host $Detail1.metadata.ocrText.Substring(0, [Math]::Min(120, $Detail1.metadata.ocrText.Length)) "..." -ForegroundColor Gray
        Write-Host "AI Summary: " -NoNewline; Write-Host $Detail1.metadata.summary -ForegroundColor Yellow
        
        # Re-fetch Doc 2
        $Detail2 = curl.exe -s -X GET "$DocEndpoint/$Doc2_ID" -H "accept: application/json" | ConvertFrom-Json
        Write-Host "`n--- Document 2 ($File2) Results ---" -ForegroundColor Cyan
        Write-Host "OCR Text (excerpt): " -NoNewline; Write-Host $Detail2.metadata.ocrText.Substring(0, [Math]::Min(120, $Detail2.metadata.ocrText.Length)) "..." -ForegroundColor Gray
        Write-Host "AI Summary: " -NoNewline; Write-Host $Detail2.metadata.summary -ForegroundColor Yellow
        break
    }
}

if (-not ($Doc1_Done -and $Doc2_Done)) { Log-Warn "Timeout waiting for background processing." }

Pause-Step

# --- 5. Full Text Search ---
Log-Info "STEP 4: Full-Text Search (ElasticSearch)"

# Search 1: Technical term
$Term1 = "RabbitMQ"
Log-Info "Action: Search for '$Term1' (Expected in $File1)"
$Res1 = curl.exe -s -X GET "$DocEndpoint/search?query=$Term1&mode=content" -H "accept: application/json" | ConvertFrom-Json
if ($Res1.Count -ge 1) { 
    Log-Success "Found: $($Res1[0].fileName)" 
    Log-Response $Res1
} else { 
    Log-Warn "No results found for $Term1" 
}

# Search 2: Business term
$Term2 = "Cabling"
Log-Info "Action: Search for '$Term2' (Expected in $File2)"
$Res2 = curl.exe -s -X GET "$DocEndpoint/search?query=$Term2&mode=content" -H "accept: application/json" | ConvertFrom-Json
if ($Res2.Count -ge 1) { 
    Log-Success "Found: $($Res2[0].fileName)" 
    Log-Response $Res2
} else { 
    Log-Warn "No results found for $Term2" 
}

Pause-Step

# --- 6. Add Note (FIXED) ---
Log-Info "STEP 5: Manage Notes"
Log-Info "Adding note to $Doc2_Name (Invoice)..."

# FIX: Use a temporary file to safely pass JSON with spaces to curl
$NoteObj = @{ text = "Approved for payment by IT department." }
$NoteJson = $NoteObj | ConvertTo-Json -Compress
$NoteJson | Out-File "note_payload.json" -Encoding ASCII

$NoteRes = curl.exe -s -X POST "$DocEndpoint/$Doc2_ID/notes" `
    -H "Content-Type: application/json" `
    -d "@note_payload.json" | ConvertFrom-Json

# Clean up
if (Test-Path "note_payload.json") { Remove-Item "note_payload.json" }

Log-Success "Note Added"
Log-Response $NoteRes

Pause-Step

# --- 7. Update Metadata (FIXED) ---
Log-Info "STEP 6: Update Document Metadata"
Log-Info "Updating tags for $Doc1_Name..."

# FIX: Use a temporary file here too
$UpdateObj = @{
    fileName    = $Doc1_Name
    fileSize    = 0
    contentType = "application/pdf"
    tags        = @("architecture", "finalized", "approved_v2")
}
$UpdateJson = $UpdateObj | ConvertTo-Json -Compress
$UpdateJson | Out-File "update_payload.json" -Encoding ASCII

$UpdateRes = curl.exe -s -X PUT "$DocEndpoint/$Doc1_ID" `
    -H "Content-Type: application/json" `
    -d "@update_payload.json" | ConvertFrom-Json

# Clean up
if (Test-Path "update_payload.json") { Remove-Item "update_payload.json" }

Log-Success "Tags Updated"
Log-Response $UpdateRes

Pause-Step

# --- 8. Delete Document ---
Log-Info "STEP 7: Delete Document"
Log-Info "Deleting $Doc2_Name..."

$DelStatus = curl.exe -s -o NUL -w "%{http_code}" -X DELETE "$DocEndpoint/$Doc2_ID"

if ($DelStatus -eq "204" -or $DelStatus -eq "200") {
    Log-Success "Document Deleted (HTTP $DelStatus)"
} else {
    Log-Error "Delete Failed (HTTP $DelStatus)"
}

# Verify
$VerifyStatus = curl.exe -s -o NUL -w "%{http_code}" "$DocEndpoint/$Doc2_ID"
if ($VerifyStatus -eq "404") { Log-Success "Verification: Document Not Found (404)" }

Pause-Step

# --- Cleanup ---
Remove-Item $File1
Remove-Item $File2
Log-Info "=== Demo Complete ==="