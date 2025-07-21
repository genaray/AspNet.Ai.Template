# ASP.NET Platform-Engineering Template with OpenTelemetry, Kubernetes and Terraform

## Overview
This is an ASP.NET Core 8 Project consisting of two Microservices with OpenTelemetry integration for collecting metrics, traces, and logs.
The collected data is sent to an OpenTelemetry Collector, which forwards it to Prometheus, Tempo, and Loki. Grafana is used for visualization.
The respective services (AuthenticationService, UserService and Grafana stack) are orchestrated by Kubernetes and brought directly into the AWS cloud by Terraform.
An Ansible playbook is also provided to set up the local development environment ready to run the project.

The Project includes:
- **Ansible** to prepare the local development environment.
- **AuthenticationService** a service that uses AspNet.Identity to handle authentication and send emails.
- **UserService** a service that stores and retrieves user data for demonstration purposes. Communicates with the AuthenticationService.
- **Docker** Files for the respective services and a Docker compose to run everything easily.
- **Otel** to collect, process and visualize metrics, traces and logs from the services in Grafana (with Prometheus, Loki, Tempo).
- **Kubernetes** Pods, deployment, services and co to orchestrate, scale and run everything.
- **Terraform** to create AWS resources (including EKS) and populate them with the Kubernetes services.

## Prerequisites
- [Ansible](https://docs.ansible.com/ansible/latest/installation_guide/intro_installation.html#installing-and-upgrading-ansible)

## Setup & Build
### 1. Clone the Repository
```sh
git clone https://github.com/genaray/AspNet.PlatformEngineering.Template
cd AspNet.PlatformEngineering.Template
```

## 2. Setup
Execute the Ansible script in `/Ansible`:
- `cd Ansible`
- `ansible-playbook -i inventory.ini prepare_environment.yml`
This will install all the necessary dependencies on your computer (MacOS).

## 2. Configuration
Modify `appsettings.json` in `/AuthenticationService` and `/UserService` for:
- **Database Connection** (`ConnectionStrings:DefaultConnection`)
- **Email Sender Settings** (`EmailSender` section)

Modify `main.tf` in `/Terraform` for:
- **AWS-Credentials** (`access_key and secret_key`)

As well as grafana, Otel-collector, tempo, Loki and prometheus config-files at the project root for docker compose or the `/k8s` services for kubernetes.
The Kubernetes services for Grafana, Otel-Collector, Prometheus, Tempo, Loki were generated using Helm Charts and parameterized using the `_values` files. You can find the commands to do this in the respective `_values` files.

### 3. Build 
#### Building the Docker-Images
Execute the following commands in the projects root-folder.

This will build the `AuthenticationService`: 
```sh
docker build \
  -f Docker/authentication.dockerfile \
  -t authentication-service:latest \
  .
```
And this will build the `UserService`:
```sh
docker build \
  -f Docker/user.dockerfile \
  -t user-service:latest \
  .
```

### 4. Run 
#### Using Docker Compose
Execute the following commands in the `/Docker` folder:
```sh
docker-compose up --build
```
This will:
- Build and start the ASP.NET Core Project.
- Launch all observability tools and collects the metrics, traces and logs from the services.
- Start Grafana to visualize the data.

#### Using Kubernetes
Execute following commands in the `/k8s` folder:
```zsh
kubectl apply -k .
```

#### Using Terraform in the cloud
Execute the following commands in the `/Terraform` folder:
```zsh
export AWS_ACCESS_KEY_ID=AKIA…YOUR_KEY
export AWS_SECRET_ACCESS_KEY=abcd…YOUR_SECRET
terraform init
terraform apply
```

To establish a connection to the EKS cluster, you may have to adapt the `Terraform/kubeconfig.tpl` and integrate it via the cli.
The output `kubeconfig_raw` returns the kubeconfig that can be used to connect to the cluster afterwards. 

## Logging, Tracing & Metrics
The backend sends:
- **Logs** to Loki
- **Traces** to Tempo
- **Metrics** to Prometheus

Grafana reads from Prometheus, Tempo, and Loki for visualization.
Login to Grafana, add Prometheus, Tempo and Loki as Datasources and use an Otel-Dashboard of your choice.
However, the deployed grafana does not automatically display the dashboard and the datasources. You simply have to add them, import an AspNet-Otel dashboard of your choice and you're done!
Since grafana is deployed in docker (like the other services) you may not be able to address it directly via grafana's localhost but have to address it using its container name.

### Visualisation in grafana
1. Deploy the backend
2. Login to grafana http://localhost:3000 (via Docker compose or Kubernetes portforwarding) using the credentials set in the docker compose file. 
3. Add Prometheus, Tempo, Loki as Datasources
4. Import a AspNet-Otel-Dashboard of your choice (or the one included in this project)

## Accessing Services
| Service                   | URL                                    |
|---------------------------|----------------------------------------|
| API (Swagger)             | http://localhost:8080/swagger          |
| Grafana                   | http://localhost:3000                  |
| Prometheus                | http://localhost:9090                  |
| Tempo (Traces)            | http://localhost:3200                  |
| Loki (Logs)               | http://localhost:3100                  |

This is how the services can be addressed when deployed via Docker compose. In the case of Kubernetes, either further port adaptations or port forwarding are necessary.

## Outlook
This project demonstrates a simple platform engineering template. 
In the future, this could be complemented by a customized CI/CD pipeline to test the project, scan for security vulnerabilities (SonarQube, OWASP) and easily deploy to the cloud. 
Secrets and credentials could also be injected from the pipeline. As an alternative to Grafana self-hosted, the project could also be connected to the Grafana cloud to reduce complexity. 
Instead of using AspNet.Identity's own login mechanism, you could rely on a provider such as KeyCloak or communicate directly with AWS-Cognito.

## Contributing
Feel free to submit pull requests or open issues!

## License
Apache2.0 License

