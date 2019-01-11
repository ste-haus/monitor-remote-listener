# Monitor Remote

A simply little utility to remotely turn a monitor on and off via TCP commands.

Note: This _not_ a secure service. I wrote it to control some home automation stuff at home and it should under no circumstances be used in a production environment.

#### Usage:

`echo "[on|off|standby|shake|slash]" | nc :hostname :port`
