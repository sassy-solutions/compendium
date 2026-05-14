# Coding-agent sandbox base image.
#
# Hosts an ephemeral, non-root environment for coding agents
# (Claude Code, Codex, Gemini, OpenCode) driven by Compendium's
# KubernetesAgentSandbox adapter via `kubectl exec`.
#
# Image stays intentionally lean: bash + git + curl + node + python + an
# optional .NET SDK overlay. Agent CLIs are *not* installed here; the runtime
# injects them from a sidecar or downloads them per-run.
#
# Build:
#   docker build -f deploy/sandbox/coding-agent.Dockerfile \
#                -t ghcr.io/sassy-solutions/compendium/coding-agent-sandbox:1.0.0 \
#                deploy/sandbox

ARG DEBIAN_TAG=12-slim
FROM debian:${DEBIAN_TAG} AS base

ENV DEBIAN_FRONTEND=noninteractive \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    PATH=/usr/local/bin:/usr/bin:/bin:/home/agent/.local/bin

# Pinned toolchain. Versions match Debian 12 stable; bump deliberately.
ARG NODE_MAJOR=20
ARG PYTHON_PACKAGE=python3.11

RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        bash \
        ca-certificates \
        coreutils \
        curl \
        git \
        gnupg \
        jq \
        ${PYTHON_PACKAGE} \
        python3-pip \
        tini \
        tzdata \
        unzip \
        xz-utils; \
    curl -fsSL https://deb.nodesource.com/setup_${NODE_MAJOR}.x | bash -; \
    apt-get install -y --no-install-recommends nodejs; \
    apt-get clean; \
    rm -rf /var/lib/apt/lists/*

# Optional .NET SDK overlay — opt in by setting INSTALL_DOTNET=true at build time.
ARG INSTALL_DOTNET=false
ARG DOTNET_VERSION=9.0
RUN if [ "$INSTALL_DOTNET" = "true" ]; then \
        set -eux; \
        curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh; \
        chmod +x /tmp/dotnet-install.sh; \
        /tmp/dotnet-install.sh --channel ${DOTNET_VERSION} --install-dir /usr/share/dotnet; \
        ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet; \
        rm /tmp/dotnet-install.sh; \
    fi

# Non-root user. UID/GID match KubernetesSandboxOptions defaults (10001:10001)
# so PVC fsGroup / NetworkPolicy targeting works out of the box.
ARG AGENT_UID=10001
ARG AGENT_GID=10001
RUN groupadd --system --gid ${AGENT_GID} agent && \
    useradd  --system --uid ${AGENT_UID} --gid ${AGENT_GID} \
             --home /home/agent --create-home --shell /bin/bash agent && \
    mkdir -p /workspace && \
    chown ${AGENT_UID}:${AGENT_GID} /workspace

USER agent:agent
WORKDIR /workspace

# `tini` reaps zombies if a coding-agent CLI forks subprocesses. The default
# command is overridden by the sandbox adapter, which keeps PID 1 alive with
# `sleep infinity` and drives the pod through `kubectl exec`.
ENTRYPOINT ["/usr/bin/tini", "--"]
CMD ["bash", "-lc", "exec sleep infinity"]
