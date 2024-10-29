# Use NVIDIA CUDA base image for GPU support
FROM nvidia/cuda:12.6.2-cudnn-devel-ubuntu22.04

# Install dependencies
RUN apt-get update && apt-get install -y \
    libglu1-mesa \
    libxcursor1 \
    libxrandr2 \
    libc6-dev \
    libgcc1 \
    libgl1-mesa-dev \
    libglu1-mesa-dev \
    libsm6 \
    libxext6 \
    libxrender1 \
    libvulkan1 \
    vulkan-utils \
    && rm -rf /var/lib/apt/lists/*


# Set the working directory in the container
WORKDIR /app

# Copy the Unity build files to the container
COPY . /app

# Make the Unity executable file runnable
RUN chmod +x /app/BlueprintParserServerTestBuild.x86_64

# Expose the port your server will listen on (adjust if needed)
EXPOSE 8080

# Command to run the Unity server
CMD ["./BlueprintParserServerTestBuild.x86_64", "-batchmode", "-nographics"]