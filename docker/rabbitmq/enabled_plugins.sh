# Enable needed plugins on first boot.
# rabbitmq-plugins is idempotent — safe to run every time.
rabbitmq-plugins enable rabbitmq_management rabbitmq_management_agent rabbitmq_shovel rabbitmq_shovel_management
