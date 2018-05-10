# Service path in the VM.
$servicePath = "c:\3Dtoolkit\binaries\MultithreadedServer\x64"
$webrtcConfigPath = ($servicePath + "\webrtcConfig.json")
$serviceRegisterPath = ($servicePath + "\serviceRegister.ps1")
$serviceStartPath = ($servicePath + "\serviceStart.ps1")

# Updates WebRTC config file.

$turnUri = "TURN_URI"
$turnUsername = "TURN_USERNAME"
$turnPassword = "TURN_PASSWORD"
$signalingUri = "SIGNALING_URI"
$signalingPort = "SIGNALING_PORT"
$heartbeat = "HEARTBEAT"

$configuration = "{
 `"iceConfiguration`": `"relay`",
 `"turnServer`": {
   `"uri`": `"$turnUri`",
   `"username`": `"$turnUsername`",
   `"password`": `"$turnPassword`"
 },
 `"server`": `"$signalingUri`",
 `"port`": $signalingPort,
 `"heartbeat`": $heartbeat
}"

Out-File -FilePath $webrtcConfigPath -InputObject $configuration -Encoding Ascii

# Registers service.
Invoke-Expression $serviceRegisterPath

# Starts service.
Invoke-Expression $serviceStartPath