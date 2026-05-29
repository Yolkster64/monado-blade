# run_agent_container.sh
# Script to build and run Hermes agent container

docker build -t hermes-agent .
docker run -it --rm hermes-agent
