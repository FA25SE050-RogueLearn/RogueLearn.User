// Jenkinsfile (for Harbor and Kubernetes)

pipeline {
    // This tells Jenkins to create a fresh Kubernetes Pod for every pipeline run.
    agent {
        kubernetes {
            // This YAML defines the pod with all the tools we need.
            yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:9.0
    command: [sleep]
    args: [99d]
  - name: docker
    image: docker:24.0
    command: [sleep]
    args: [99d]
    volumeMounts:
    - name: dockersock
      mountPath: /var/run/docker.sock
  - name: kustomize
    image: alpine/k8s:1.28.4
    command: [sleep]
    args: [99d]
  volumes:
  - name: dockersock
    hostPath:
      path: /var/run/docker.sock
"""
        }
    }

    // Define environment variables to make the script cleaner
    environment {
        HARBOR_URL           = "harbor.roguelearn.site" // IMPORTANT: Replace with your Harbor domain
        HARBOR_PROJECT       = "roguelearn"
        APP_IMAGE_NAME       = "roguelearn-user-api"
        HARBOR_IMAGE_PATH    = "${HARBOR_URL}/${HARBOR_PROJECT}/${APP_IMAGE_NAME}"
        KUSTOMIZE_BASE_IMAGE = "soybean2610/roguelearn-user-api" // The original image name in your kustomize files
    }

    stages {
        stage('Build and Test') {
            steps {
                // Run these steps inside the 'dotnet' container
                container('dotnet') {
                    sh 'dotnet restore'
                    sh 'dotnet build --configuration Release --no-restore'
                    sh 'dotnet test --configuration Release --no-build --verbosity normal'
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                // Run these steps inside the 'docker' container
                container('docker') {
                    // Use the 'harbor-credentials' we created in Jenkins
                    withCredentials([usernamePassword(credentialsId: 'harbor-credentials', usernameVariable: 'HARBOR_USER', passwordVariable: 'HARBOR_PASS')]) {
                        sh "echo $HARBOR_PASS | docker login ${HARBOR_URL} -u $HARBOR_USER --password-stdin"
                        sh "docker build -t ${HARBOR_IMAGE_PATH}:${env.BRANCH_NAME} -t ${HARBOR_IMAGE_PATH}:${env.GIT_COMMIT.substring(0,8)} -t ${HARBOR_IMAGE_PATH}:latest -f src/RogueLearn.User.Api/Dockerfile ."
                    }
                }
            }
        }

        stage('Push to Harbor') {
            // This stage only runs for pushes to the 'main' branch
            when { branch 'main' }
            steps {
                container('docker') {
                    withCredentials([usernamePassword(credentialsId: 'harbor-credentials', usernameVariable: 'HARBOR_USER', passwordVariable: 'HARBOR_PASS')]) {
                        sh "echo $HARBOR_PASS | docker login ${HARBOR_URL} -u $HARBOR_USER --password-stdin"
                        sh "docker push --all-tags ${HARBOR_IMAGE_PATH}"
                    }
                }
            }
        }

        stage('Update K8s Manifests') {
            when { branch 'main' }
            steps {
                // Run these steps inside the 'kustomize' container
                container('kustomize') {
                    // Use the 'github-token' we created in Jenkins
                    withCredentials([string(credentialsId: 'github-token', variable: 'GIT_TOKEN')]) {
                        sh """
                        # Clone, configure git, and run kustomize
                        git clone https://oauth2:${GIT_TOKEN}@github.com/FA25SE050-RogueLearn/RogueLearn.Kubernetes.git k8s-manifests
                        cd k8s-manifests
                        git config user.name "Jenkins CI"
                        git config user.email "jenkins@your-domain.com"

                        cd roguelearn-user-api/base
                        kustomize edit set image ${KUSTOMIZE_BASE_IMAGE}=${HARBOR_IMAGE_PATH}:${env.GIT_COMMIT.substring(0,8)}
                        cd ../..

                        # Commit and push if there are changes
                        git add .
                        if ! git diff --staged --quiet; then
                          git commit -m "ci: Update image for roguelearn-user-api to ${env.GIT_COMMIT.substring(0,8)} (from Harbor)"
                          git push
                        else
                          echo "No changes to commit"
                        fi
                        """
                    }
                }
            }
        }
    }

    post {
        // This block runs at the end of every pipeline run
        always {
            echo 'Pipeline finished.'
            cleanWs() // Cleans up the workspace to save disk space
        }
    }
}