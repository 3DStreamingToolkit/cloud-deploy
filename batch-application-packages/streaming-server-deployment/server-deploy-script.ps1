$TURN_URI = $args[0]
$TURN_USERNAME= $args[1]
$TURN_PASSWORD= $args[2]
$SIGNALING_URI= $args[3]
$SIGNALING_PORT= $args[4]
$HEARTBEAT= $args[5]
$capacity = $args[6]
$servicePath = $args[7]

# Service path in the VM.
$webrtcConfigPath = ($servicePath + "\webrtcConfig.json")
$serviceConfigPath = ($servicePath + "\serverConfig.json")
$serviceRegisterPath = ($servicePath + "\serviceRegister.ps1")
$serviceStartPath = ($servicePath + "\serviceStart.ps1")

# Updates WebRTC config file.
$configuration = "{
 `"iceConfiguration`": `"relay`",
 `"turnServer`": {
   `"uri`": `"$TURN_URI`",
   `"username`": `"$TURN_USERNAME`",
   `"password`": `"$TURN_PASSWORD`"
 },
 `"server`": `"$SIGNALING_URI`",
 `"port`": $SIGNALING_PORT,
 `"heartbeat`": $HEARTBEAT
}"

# Updates service config file.
$serviceconfiguration = "{
  `"serverConfig`": {
    `"width`": 1280,
    `"height`": 720,
    `"systemService`": true,
    `"systemCapacity`": $capacity,
    `"autoCall`": true,
    `"autoConnect`":  true
  },
  `"serviceConfig`": {
    `"name`": `"3DStreamingRenderingService`",
    `"displayName`": `"3D Streaming Rendering Service`",
    `"serviceAccount`": `"NT AUTHORITY\\NetworkService`",
    `"servicePassword`": null
  }
}"

Out-File -FilePath $webrtcConfigPath -InputObject $configuration -Encoding Ascii
Out-File -FilePath $serviceConfigPath -InputObject $serviceconfiguration -Encoding Ascii


# Registers service.
Invoke-Expression $serviceRegisterPath

# Starts service.
Invoke-Expression $serviceStartPath