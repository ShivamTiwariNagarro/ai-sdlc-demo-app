param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("userPromptSubmitted", "postToolUse", "subagentStop", "agentStop")]
    [string]$Event
)

$ErrorActionPreference = "Stop"

$rawInput = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($rawInput)) {
    throw "Copilot did not provide the expected JSON event payload on stdin."
}

$payload = $rawInput | ConvertFrom-Json
if ($null -eq $payload -or $payload -isnot [pscustomobject]) {
    throw "Expected the Copilot hook event payload to be a JSON object."
}

function Get-Field {
    param(
        [object]$InputObject,
        [string[]]$Names,
        [object]$Default = $null
    )

    foreach ($name in $Names) {
        $property = $InputObject.PSObject.Properties[$name]
        if ($null -ne $property -and $null -ne $property.Value) {
            return $property.Value
        }
    }

    return $Default
}

$timestamp = [DateTime]::UtcNow
$toolsUsed = @(
    Get-Field -InputObject $payload -Names @("tools_used", "toolsUsed") -Default @()
)
$toolName = Get-Field -InputObject $payload -Names @("toolName", "tool_name")
if ($null -ne $toolName -and $toolName -notin $toolsUsed) {
    $toolsUsed += $toolName
}

$request = $null
if ($Event -eq "userPromptSubmitted") {
    $request = Get-Field -InputObject $payload -Names @(
        "prompt",
        "userPrompt",
        "user_prompt",
        "userMessage",
        "user_message",
        "message",
        "promptText",
        "prompt_text"
    )
}

$stage = Get-Field -InputObject $payload -Names @("stage")
if ([string]::IsNullOrWhiteSpace([string]$stage)) {
    $stagePath = Join-Path (Get-Location) "logs/current-stage.txt"
    if (Test-Path -LiteralPath $stagePath) {
        $stage = (Get-Content -LiteralPath $stagePath -Raw).Trim()
        if ([string]::IsNullOrWhiteSpace($stage)) {
            $stage = $null
        }
    }
}

$agentName = Get-Field -InputObject $payload -Names @("agent_name", "agentName")
$agentType = Get-Field -InputObject $payload -Names @("agent_type", "agentType")
$agent = Get-Field -InputObject $payload -Names @("agent")
if ($null -eq $agent -and $null -ne $agentName) {
    $agent = $agentName
}

$record = [ordered]@{
    id = "$($timestamp.ToString('yyyyMMddTHHmmssZ'))-$Event-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
    timestamp = $timestamp.ToString("o")
    event = $Event
    stage = $stage
    agent = $agent
    agent_name = $agentName
    agent_type = $agentType
    ai_tool = Get-Field -InputObject $payload -Names @("ai_tool", "aiTool") -Default "GitHub Copilot"
    model = Get-Field -InputObject $payload -Names @("model", "model_name", "modelName")
    mode = Get-Field -InputObject $payload -Names @("mode")
    related_ids = @(Get-Field -InputObject $payload -Names @("related_ids", "relatedIds") -Default @())
    parent_task = Get-Field -InputObject $payload -Names @("parent_task", "parentTask")
    context = Get-Field -InputObject $payload -Names @("context") -Default "AI in SDLC Assignment"
    request = $request
    skills_used = @(Get-Field -InputObject $payload -Names @("skills_used", "skillsUsed") -Default @())
    subagents_used = @(Get-Field -InputObject $payload -Names @("subagents_used", "subagentsUsed") -Default @())
    mcp_tools_used = @(Get-Field -InputObject $payload -Names @("mcp_tools_used", "mcpToolsUsed") -Default @())
    tools_used = $toolsUsed
    suggestion = Get-Field -InputObject $payload -Names @("suggestion")
    human_decision = Get-Field -InputObject $payload -Names @("human_decision", "humanDecision")
    action_verification = Get-Field -InputObject $payload -Names @("action_verification", "actionVerification")
    limitation = "Hook events only record the data provided by Copilot; add missing model, skill, human-decision, review, and test evidence manually. Set logs/current-stage.txt before each assignment phase."
    status = "captured"
    artifacts = @(Get-Field -InputObject $payload -Names @("artifacts") -Default @())
    event_payload = $payload
}

New-Item -ItemType Directory -Force -Path "logs" | Out-Null
$jsonLine = $record | ConvertTo-Json -Depth 50 -Compress
$logPath = Join-Path (Get-Location) "logs/ai-interactions.jsonl"
$utf8WithoutBom = New-Object System.Text.UTF8Encoding -ArgumentList $false
$writer = New-Object System.IO.StreamWriter($logPath, $true, $utf8WithoutBom)
try {
    $writer.WriteLine($jsonLine)
}
finally {
    $writer.Dispose()
}

Write-Output "{}"