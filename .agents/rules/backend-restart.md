---
description: Always restart the backend after code changes
---

# Backend Restart Rule

- **Constraint**: The backend does not support hot-reloading. Whenever you make modifications to any backend code (.cs files, configurations, etc.), you MUST restart the backend server for the changes to take effect.
- **Action**: Identify the running backend task using the `manage_task` tool, kill it, and start it again, or follow the workspace's established process for restarting the backend before verifying your changes.
