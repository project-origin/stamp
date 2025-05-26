#!/bin/bash

# This script is used to test the stamp chart using kind.
# It installs the chart and validates it starts up correctly.

# Define kind cluster name
cluster_name=stamp-test

# Ensures script fails if something goes wrong.
set -eo pipefail

# define cleanup function
cleanup() {
    rm -fr $temp_folderx
    kind delete cluster -n ${cluster_name} >/dev/null 2>&1
}

# define debug function
debug() {
    echo -e "\nDebugging information:"
    echo -e "\nHelm status:"
    helm status stamp -n stamp --show-desc --show-resources

    echo -e "\nDeployment description:"
    kubectl describe deployment -n stamp po-stamp-deployment

    POD_NAMES=$(kubectl get pods -n stamp -l app=po-stamp -o jsonpath="{.items[*].metadata.name}")
    # Loop over the pods and print their logs
    for POD_NAME in $POD_NAMES
    do
        echo -e "\nLogs for $POD_NAME:"
        kubectl logs -n stamp $POD_NAME
    done
}

# trap cleanup function on script exit
trap 'cleanup' 0
trap 'debug; cleanup' ERR

# define variables
temp_folder=$(mktemp -d)
values_filename=${temp_folder}/values.yaml

# create kind cluster
kind delete cluster -n ${cluster_name}
kind create cluster -n ${cluster_name}

# install rabbitmq-operator
kubectl apply -f "https://github.com/rabbitmq/cluster-operator/releases/download/v2.5.0/cluster-operator.yml"

# install postgresql
helm install postgresql oci://registry-1.docker.io/bitnamicharts/postgresql --version 15.5.23 --kube-context kind-${cluster_name} --namespace stamp --create-namespace --wait

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

postgresql:
  host: postgresql
  database: postgres
  username: postgres
  port: 5432
  password:
    secretRef:
      name: postgresql
      key: postgres-password
EOF

# install stamp chart
helm install stamp ./chart --values ${values_filename} --namespace stamp --wait

echo "Test completed"
