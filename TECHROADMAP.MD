# Unity Blueprint Parsing Server Roadmap

## Project Summary
This project is a Unity-based, Docker-deployed server-client system that processes blueprint images (black on white) into 2D matrices representing wall data. Using an HLSL compute shader for GPU-accelerated processing, the server handles multiple parallel requests, similar to a REST API. This setup enables scalability across multiple servers using Docker, with potential for future mesh conversion of the parsed matrix data.

---

## Roadmap

### 1. Initial Setup

1. **Create Unity Project in Headless Mode**
   - Open Unity, create a new **3D project**, and name it descriptively (e.g., `BlueprintParserServer`).
   - Set Unity to **headless mode**:
     - Go to **Edit > Project Settings > Player > Resolution and Presentation**.
     - Enable **Run in Background** and remove unnecessary graphics settings.

2. **Implement Basic REST API Structure**
   - Choose your API framework:
     - **ASP.NET Core** or **Node.js** for a standalone API, or
     - **UnityWebRequest** or `Best HTTP/2` plugin within Unity.
   - Define API routes:
     - `/parseBlueprint` for parsing requests.
     - `/status` for server status checks.

3. **Create Request and Response Handlers**
   - Implement `BlueprintRequestHandler` to receive images and return matrix or error responses.
   - Test locally with a REST client (e.g., Postman) to confirm request handling.

---

### 2. Implement HLSL Compute Shader for Parsing

1. **Set Up Basic HLSL Shader**
   - Create a new compute shader (e.g., `BlueprintParser.compute`).
   - Define `texture` and `buffer` inputs to load blueprint images and output parsed data.

2. **Write Initial Shader Code for Basic Image Processing**
   - Process each pixel to determine if it represents a wall (white) or space (black).
   - Output a 2D matrix with values (1 for walls, 0 for spaces).
   - Add adjustable parameters for thresholding.

3. **Integrate Compute Shader in Unity**
   - Write a `BlueprintParser` script to send image data to the shader and read back the matrix buffer.
   - Test with sample images to confirm accurate output.

4. **Optimize Shader for Performance**
   - Use tile-based parallel processing (e.g., `16x16` pixel blocks).
   - Profile GPU usage to ensure efficiency.

---

### 3. Dockerize the Unity Server

1. **Build Unity Server for Linux Headless**
   - Go to **File > Build Settings**, select **Linux**, and enable **Headless Mode**.
   - Set the build target to `x86_64`, build, and test it locally.

2. **Create Dockerfile**
   - Create a Dockerfile to set up the Unity environment:
     ```dockerfile
     FROM nvidia/cuda:11.3.0-base
     WORKDIR /app
     COPY ./YourUnityBuild /app
     CMD ["./YourUnityServer"]
     ```

3. **Enable GPU Access with NVIDIA Docker Toolkit**
   - Install **NVIDIA Container Toolkit** on the host machine.
   - Run the container with GPU access:
     ```bash
     docker run --gpus all -it your_unity_container
     ```

4. **Test Container Functionality**
   - Confirm the container can access the GPU, process images, and handle requests.
   - Test by sending image data and verifying matrix output.

---

### 4. Implement Parallel Request Handling

1. **Make Parsing Requests Asynchronous**
   - Use `async`/`await` in C# or coroutines to manage non-blocking requests.
   - Each request creates a task or coroutine to ensure independent processing.

2. **Handle State and Resource Management**
   - Ensure stateless processing by creating separate buffers for each request.
   - Test with concurrent requests to check performance and response times.

3. **Queue Management and Buffering (Optional)**
   - Use a lightweight queuing system to delay requests when resources are full.
   - Implement a job system or queue within the request handler if necessary.

---

### 5. Scale with Docker Compose or Kubernetes

1. **Create Docker Compose File (for Local Scaling)**
   - Define a `docker-compose.yml` for multiple container instances with shared environment variables.

2. **Deploy with Kubernetes for High Scalability**
   - Set up Kubernetes configurations for scaling.
   - Add a load balancer to distribute client requests.
   - Implement horizontal autoscaling to adjust instances based on load.

---

### 6. Future Expansion: Mesh Generation from Matrix

1. **Convert Matrix Data to Mesh Structure**
   - Create a `MatrixToMeshConverter` script that generates 3D mesh objects based on matrix data.
   - Use quads or custom models for mesh creation.

2. **Optimize Mesh for Large Floor Plans**
   - Use mesh merging to reduce draw calls and optimize performance.
   - Apply smoothing or simplification if needed.

3. **API Extension for Mesh Data Retrieval (Optional)**
   - Add a route (e.g., `/getMeshData`) to allow clients to retrieve mesh data for visualization.

---

## Notes
This roadmap outlines each phase of development for clarity and organization. Expand with additional features or optimizations as needed based on performance and project requirements.
