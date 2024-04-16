#!/bin/bash

# This script is used to test the stamp chart using kind.
# It installs the chart and validates it starts up correctly.

# Define kind cluster name
cluster_name=stamp-test

# Ensures script fails if something goes wrong.
set -eo pipefail

# cleanup - delete temp_folder and cluster
trap 'rm -fr $temp_folder; kind delete cluster -n ${cluster_name} >/dev/null 2>&1' 0

# define variables
temp_folder=$(mktemp -d)
values_filename=${temp_folder}/values.yaml

# create kind cluster
kind delete cluster -n ${cluster_name}
kind create cluster -n ${cluster_name}

# install rabbitmq-operator
kubectl apply -f "https://github.com/rabbitmq/cluster-operator/releases/download/v2.5.0/cluster-operator.yml"

# install cnpg-operator
helm install cnpg-operator cloudnative-pg --repo https://cloudnative-pg.io/charts --version 0.18.0 --namespace cnpg --create-namespace --wait

# build docker image
docker build -f src/Stamp.Dockerfile -t ghcr.io/project-origin/stamp:test src/

# load docker image into cluster
kind load -n ${cluster_name} docker-image ghcr.io/project-origin/stamp:test

# generate values.yaml file
cat << EOF > "${values_filename}"
image:
  tag: test

messageBroker:
  type: rabbitmqOperator
EOF

# install stamp chart
helm install stamp ./chart --values ${values_filename} --namespace stamp --create-namespace --wait

echo "Test completed"
