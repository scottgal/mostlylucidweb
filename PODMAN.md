# Podman Usage Guide

This guide explains how to use Podman as an alternative to Docker for running the Mostlylucid blog platform.

## What is Podman?

Podman is a daemonless container engine for developing, managing, and running OCI Containers on your Linux system. It's designed as a drop-in replacement for Docker with enhanced security features and rootless container support.

## Prerequisites

### Installing Podman

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install -y podman
```

**Fedora/RHEL:**
```bash
sudo dnf install -y podman
```

**macOS:**
```bash
brew install podman
podman machine init
podman machine start
```

**Windows:**
Download from [Podman official website](https://podman.io/getting-started/installation) and follow the installer instructions.

### Installing podman-compose

```bash
pip3 install podman-compose
```

Or on some systems:
```bash
sudo apt-get install podman-compose
```

## Key Differences from Docker

1. **Daemonless Architecture**: Podman doesn't require a background daemon running with root privileges
2. **Rootless Containers**: Run containers without root access for improved security
3. **SELinux Labels**: Volume mounts use `:Z` suffix for proper SELinux context
4. **Pod Support**: Native support for Kubernetes-style pods
5. **Socket Location**: Uses `/run/podman/podman.sock` instead of `/var/run/docker.sock`
6. **Systemd Integration**: Better integration with systemd for service management

## Running the Application

### Production Stack

```bash
# Start all services
podman-compose -f podman-compose.yml up -d

# View logs
podman-compose -f podman-compose.yml logs -f mostlylucid

# Stop all services
podman-compose -f podman-compose.yml down

# Rebuild and restart
podman-compose -f podman-compose.yml up -d --build
```

### Development Dependencies

```bash
# Start only development dependencies (database, SMTP)
podman-compose -f devdeps-podman-compose.yml up -d

# Stop development dependencies
podman-compose -f devdeps-podman-compose.yml down
```

## Building Images with Podman

The existing Dockerfiles work seamlessly with Podman:

```bash
# Build the main application
podman build -t scottgal/mostlylucid:latest -f Mostlylucid/Dockerfile .

# Build the scheduler service
podman build -t scottgal/mostlylucid-scheduler:latest -f Mostlylucid.SchedulerService/Dockerfile .
```

## Rootless Containers

One of Podman's key advantages is rootless operation:

```bash
# Run rootless (default for non-root users)
podman-compose -f podman-compose.yml up -d

# Check container user
podman top mostlylucid user
```

### Volume Permissions with Rootless

When running rootless, ensure host directories have proper permissions:

```bash
# Create directories with proper ownership
mkdir -p /mnt/imagecache /mnt/logs /mnt/markdown
sudo chown -R $(id -u):$(id -g) /mnt/imagecache /mnt/logs /mnt/markdown
```

## Auto-Updates (Alternative to Watchtower)

Watchtower doesn't work with Podman. Instead, use Podman's built-in auto-update feature:

### Using Podman Auto-Update

1. **Label your containers** in the compose file:
```yaml
labels:
  - "io.containers.autoupdate=registry"
```

2. **Enable systemd timer** for automatic updates:
```bash
# Create systemd service for your compose stack
podman generate systemd --new --files --name mostlylucid

# Move service files to systemd directory
sudo mv *.service /etc/systemd/system/

# Enable auto-update timer
sudo systemctl enable --now podman-auto-update.timer

# Check timer status
sudo systemctl status podman-auto-update.timer
```

3. **Manual update check**:
```bash
podman auto-update
```

## Systemd Integration

Run your entire stack as a systemd service:

### Generate Systemd Service

```bash
# Generate service file from running compose stack
cd /home/user/mostlylucidweb
podman-compose -f podman-compose.yml up -d
podman generate systemd --new --files --name -t 5 mostlylucid

# Install service
sudo mv container-mostlylucid.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable container-mostlylucid.service
sudo systemctl start container-mostlylucid.service
```

### Manage Service

```bash
# Check status
sudo systemctl status container-mostlylucid.service

# View logs
sudo journalctl -u container-mostlylucid.service -f

# Restart
sudo systemctl restart container-mostlylucid.service
```

## Networking

Podman networking works similarly to Docker:

```bash
# List networks
podman network ls

# Inspect network
podman network inspect app_network

# Create custom network
podman network create my_network
```

## Troubleshooting

### SELinux Issues

If you encounter permission issues with volumes:

```bash
# Temporarily set SELinux to permissive (not recommended for production)
sudo setenforce 0

# Or fix labels properly
sudo chcon -Rt svirt_sandbox_file_t /mnt/imagecache
```

### Port Binding as Rootless

Ports below 1024 require special handling:

```bash
# Allow binding to privileged ports
sudo sysctl net.ipv4.ip_unprivileged_port_start=80

# Or use port mapping
# Map port 8080 to 80 using firewall rules or reverse proxy
```

### Compose Compatibility

If `podman-compose` has issues, try using Podman's native docker-compose support:

```bash
# Use podman as docker socket replacement
systemctl --user enable --now podman.socket
export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock

# Now use regular docker-compose
docker-compose -f podman-compose.yml up -d
```

## Migration from Docker

To migrate from Docker to Podman:

1. **Stop Docker containers**:
```bash
docker-compose down
```

2. **Export Docker volumes** (if needed):
```bash
docker run --rm -v docker_volume:/data -v $(pwd):/backup alpine tar czf /backup/volume-backup.tar.gz -C /data .
```

3. **Import to Podman volumes**:
```bash
podman volume create grafana-data
podman run --rm -v grafana-data:/data -v $(pwd):/backup alpine tar xzf /backup/volume-backup.tar.gz -C /data
```

4. **Start with Podman**:
```bash
podman-compose -f podman-compose.yml up -d
```

## Performance Considerations

- **cgroups v2**: Ensure your system uses cgroups v2 for best performance
- **Storage Driver**: `overlay2` is recommended (default on most systems)
- **Network Performance**: Podman's CNI networking has similar performance to Docker's bridge network

## Security Benefits

1. **No Root Daemon**: Eliminates a major attack surface
2. **User Namespaces**: Containers run in isolated user namespaces
3. **SELinux Integration**: Better integration with SELinux for mandatory access control
4. **Audit Logging**: Better integration with Linux audit subsystem
5. **No Group Membership**: Don't need to add users to 'docker' group

## Resources

- [Podman Official Documentation](https://docs.podman.io/)
- [Podman Compose](https://github.com/containers/podman-compose)
- [Transitioning from Docker to Podman](https://podman.io/getting-started/transition)
- [Podman Desktop](https://podman-desktop.io/) - GUI alternative to Docker Desktop
