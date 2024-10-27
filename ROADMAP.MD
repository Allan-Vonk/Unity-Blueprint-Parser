# Project Roadmap

## Project Overview
The Unity server-client system parses black-and-white blueprint images into 2D matrices indicating wall structures. Built with scalability and performance in mind, the project leverages:
- **HLSL compute shaders** for GPU-accelerated parallel processing.
- **Docker** for containerization, enabling scalable deployment.
- A **stateless REST-like API** structure for handling requests in parallel.

## Milestones

### Phase 1: Core Setup and Blueprint Parsing
- [ ] **Set up Unity project** with headless mode and core components.
- [ ] **Implement HLSL compute shader** to parse blueprint images into binary matrices (e.g., `1` for walls, `0` for empty spaces).
  - [ ] Define image processing parameters (resolution, wall/space threshold, etc.).
- [ ] **Create a REST API structure**:
  - [ ] Design API endpoints for sending blueprint images and retrieving 2D matrix data.
  - [ ] Implement request handling within Unity or link to a lightweight HTTP server.

### Phase 2: Docker Integration and Parallel Request Handling
- [ ] **Dockerize the Unity server**:
  - [ ] Write a Dockerfile with headless Unity configuration.
  - [ ] Ensure GPU access with NVIDIA Docker (Container Toolkit).
  - [ ] Test HLSL shader functionality inside Docker.
- [ ] **Implement parallel request processing**:
  - [ ] Use async or coroutines to handle concurrent API requests.
  - [ ] Verify each request is processed statelessly and does not interfere with others.

### Phase 3: Scalability and Orchestration
- [ ] **Set up Docker Compose** or **Kubernetes** for scaling:
  - [ ] Define service configurations for deploying multiple containers.
  - [ ] Set up load balancing to distribute client requests across instances.
- [ ] **Implement request queuing or buffering** (if necessary) to handle high volumes of incoming requests.

### Phase 4: Future Expansion - Mesh Generation
- [ ] **Convert matrix data into 3D mesh structures**:
  - [ ] Define 3D wall models or use Unity primitives for mesh generation.
  - [ ] Create a Unity script to transform the 2D matrix into a navigable 3D mesh.
- [ ] **Refine API to support mesh data retrieval** (if required for client visualization).

## Additional Considerations
- **Testing and Benchmarking**:
  - Test GPU performance in Docker to ensure compute shader efficiency.
  - Benchmark request handling under load to ensure the system meets performance goals.
- **Security**:
  - Consider authentication and access control if scaling to multiple users.
  - Implement error handling and logging for reliable, user-friendly API interactions.

---

## Notes
This roadmap can be expanded with additional features based on project requirements and performance outcomes.
