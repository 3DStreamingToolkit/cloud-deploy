$servicePath = "c:\3Dtoolkit\binaries\MultithreadedServer\x64"
$webrtcConfigPath = ($servicePath + "\webrtcConfig.json")

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