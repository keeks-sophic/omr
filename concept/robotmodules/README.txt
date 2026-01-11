========================================================
ROBOT MODULES (DOC INDEX)
========================================================

These files describe how each robot module works, including:
- data flow: Backend (NATS/JetStream) <-> Robot
- internal robot logic boundaries (state, tasks, commands, motion)
- reliability: durable vs droppable messages

References:
- robotconcept.txt (system design)
- rbnats.txt (robot/backend NATS subjects)
- backendmodules/* (backend-side behavior for the same subjects)

Files:
- 00_startup_identity.txt
- 01_messaging_contracts.txt
- 02_state_store.txt
- 03_task_route_execution.txt
- 04_command_handling.txt
- 05_traffic_adapter.txt
- 06_motion_control.txt
- 07_hardware_interface.txt
- 08_telemetry_reporting.txt


========================================================
END-TO-END COMMS CHECKLIST (ROBOT <-> BACKEND)
========================================================

Identity:
- robotId is a stable string (recommended: serial number)
- robotId is passed via CLI (--id) or config default
- NATS subjects are always robot.{robotId}.*

Robot subscribes (Backend -> Robot):
- Commands: cmd.grip, cmd.hoist, cmd.telescope, cmd.cam_toggle, cmd.rotate, cmd.mode
- Task/Route: task.assign, task.control, route.assign, route.update
- Config: cfg.motion_limits, cfg.runtime_mode, cfg.features
- Traffic schedule: traffic.schedule (droppable latest-wins)

Robot publishes (Robot -> Backend):
- Presence: presence.hello, presence.heartbeat
- ACK: cmd_ack
- State: state.snapshot, state.event
- Task/Route: task.event, route.progress
- Telemetry: telemetry.battery, telemetry.health, telemetry.pose, telemetry.motion, telemetry.radar, telemetry.qr
- Logs: log.event

Reliability:
- Durable: commands/tasks/routes/config + cmd_ack + state snapshot/event + task/route events
- Droppable: traffic.schedule, high-rate pose/motion suggested


========================================================
ROBOT <-> BACKEND SUBJECT MATRIX
========================================================

Legend:
- Dir: B->R (backend publishes, robot subscribes) | R->B (robot publishes, backend subscribes)
- Reliability: Durable | Droppable | Best-effort

| Subject | Dir | Reliability | Payload (concept) | Producer Module | Consumer Module |
|---|---|---|---|---|---|
| robot.{robotId}.presence.hello | R->B | Best-effort | PresenceHello | 08_telemetry_reporting | backendmodules/03_robot_registry_sessions |
| robot.{robotId}.presence.heartbeat | R->B | Best-effort | PresenceHeartbeat | 08_telemetry_reporting | backendmodules/03_robot_registry_sessions |
| robot.{robotId}.state.snapshot | R->B | Durable | RobotStateDto | 02_state_store | backendmodules/04_state_ingestion_store |
| robot.{robotId}.state.event | R->B | Durable | RobotStateEvent | 02_state_store | backendmodules/04_state_ingestion_store |
| robot.{robotId}.telemetry.battery | R->B | Mixed | BatteryTelemetry | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.telemetry.health | R->B | Mixed | HealthTelemetry | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.telemetry.pose | R->B | Mixed | PoseTelemetry | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.telemetry.motion | R->B | Mixed | MotionTelemetry | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.telemetry.radar | R->B | Mixed | RadarTelemetry | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.telemetry.qr | R->B | Mixed | QrTelemetry | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.task.event | R->B | Durable | RobotTaskEvent | 03_task_route_execution | backendmodules/05_task_manager |
| robot.{robotId}.route.progress | R->B | Durable | RouteProgress | 03_task_route_execution | backendmodules/05_task_manager |
| robot.{robotId}.log.event | R->B | Durable | RobotLogEvent | 08_telemetry_reporting | backendmodules/04_state_ingestion_store |
| robot.{robotId}.cmd_ack | R->B | Durable | CommandAck | 01_messaging_contracts | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cmd.mode | B->R | Durable | ModeCommand | 04_command_handling | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cmd.grip | B->R | Durable | GripCommand | 04_command_handling | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cmd.hoist | B->R | Durable | HoistCommand | 04_command_handling | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cmd.telescope | B->R | Durable | TelescopeCommand | 04_command_handling | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cmd.cam_toggle | B->R | Durable | CamToggleCommand | 04_command_handling | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cmd.rotate | B->R | Durable | RotateCommand | 04_command_handling | backendmodules/02_messaging_contracts_nats |
| robot.{robotId}.cfg.motion_limits | B->R | Durable | MotionLimitsConfig | 02_state_store | backendmodules/09_configuration_service |
| robot.{robotId}.cfg.runtime_mode | B->R | Durable | RuntimeModeConfig | 02_state_store | backendmodules/09_configuration_service |
| robot.{robotId}.cfg.features | B->R | Durable | FeaturesConfig | 02_state_store | backendmodules/09_configuration_service |
| robot.{robotId}.task.assign | B->R | Durable | TaskAssignment | 03_task_route_execution | backendmodules/05_task_manager |
| robot.{robotId}.task.control | B->R | Durable | TaskControl | 03_task_route_execution | backendmodules/05_task_manager |
| robot.{robotId}.route.assign | B->R | Durable | RouteAssign | 03_task_route_execution | backendmodules/05_task_manager |
| robot.{robotId}.route.update | B->R | Durable | RouteUpdate | 03_task_route_execution | backendmodules/05_task_manager |
| robot.{robotId}.traffic.schedule | B->R | Droppable | TrafficSchedule | 05_traffic_adapter | backendmodules/08_schedule_publisher |
