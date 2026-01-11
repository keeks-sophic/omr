========================================================
BACKEND MODULES (DOC INDEX)
========================================================

These files describe how each backend module works, including:
- data flow: Robot (NATS) <-> Backend <-> Frontend (REST/SignalR)
- persistence: Postgres/PostGIS + TimescaleDB replay
- main logic and failure handling

Files:
- 00_security_rbac.txt
- 01_api_realtime_gateway.txt
- 02_messaging_contracts_nats.txt
- 03_robot_registry_sessions.txt
- 04_state_ingestion_store.txt
- 05_task_manager.txt
- 06_route_planner.txt
- 07_traffic_control_service.txt
- 08_schedule_publisher.txt
- 09_configuration_service.txt
- 10_map_management_service.txt
- 11_teaching_mission_service.txt
- 12_simulation_orchestrator.txt
- 13_observability_ops_replay.txt


========================================================
END-TO-END DATA FLOW (FRONTEND <-> BACKEND <-> ROBOT)
========================================================

References:
- Frontend streams: fbstream.txt (REST + SignalR topics)
- Robot streams: rbnats.txt (NATS/JetStream subjects)
- DTOs/entities: backenddata.txt
- DB schema: dbmodel.txt (PostGIS + TimescaleDB)

Example A: Create a PICK_DROP task (point -> point)
1) Frontend POST /api/v1/tasks with fromPointId/toPointId
2) Backend (Task Manager) validates points via map.points(type=PICK_DROP)
3) Backend plans route and stores task.tasks + task.routes
4) Backend publishes NATS:
   - robot.{robotId}.task.assign
   - robot.{robotId}.route.assign
5) Robot executes and publishes feedback:
   - robot.{robotId}.route.progress, task.event, state.*, telemetry.*
6) Backend ingests and:
   - writes Timescale replay.robot_events
   - emits SignalR task.status.changed + robot.route.progress + robot.state.*

Example B: Live robot monitoring
1) Frontend joins SignalR group robot:{robotId}
2) Backend sends robot.state.snapshot and robot.telemetry.snapshot immediately
3) Backend streams robot.state.event + telemetry topics as they arrive

Example C: Teach session to create a mission
1) Frontend POST /api/v1/teach/sessions then /start
2) Backend sends cmd.mode with teachEnabled + teachSessionId (NATS durable)
3) Operator actions send commands; robot returns cmd_ack and state snapshots
4) Frontend triggers capture-step; backend stores settled RobotStateDto in task.teach_sessions
5) Save mission persists task.missions.steps and emits mission.updated
