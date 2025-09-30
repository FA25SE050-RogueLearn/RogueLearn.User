// Jenkinsfile (Corrected for Groovy backslash syntax)

pipeline {
    agent any

    environment {
        HARBOR_URL           = "harbor.roguelearn.site"
        HARBOR_PROJECT       = "roguelearn"
        APP_IMAGE_NAME       = "roguelearn-user-api"
        HARBOR_IMAGE_PATH    = "${HARBOR_URL}/${HARBOR_PROJECT}/${APP_IMAGE_NAME}"
        KUSTOMIZE_BASE_IMAGE = "soybean2610/roguelearn-user-api"
    }

    stages {
        stage('Build and Test') {
            agent {
                kubernetes {
                    yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: dotnet
    image: mcr.microsoft.com/dotnet/sdk:9.0
    command: [sleep]
    args: [99d]
"""
                }
            }
            steps {
                container('dotnet') {
                    sh 'dotnet restore'
                    sh 'dotnet build --configuration Release --no-restore'
                    sh 'dotnet test --configuration Release --no-build --verbosity normal'
                }
            }
        }

        stage('Build and Push to Harbor with Kaniko') {
            when { branch 'main' }
            agent {
                kubernetes {
                    yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: kaniko
    image: gcr.io/kaniko-project/executor:v1.10.0-debug
    imagePullPolicy: Always
    command: [sleep]
    args: [99d]
"""
                }
            }
            steps {
                container('kaniko') {
                    withCredentials([usernamePassword(credentialsId: 'harbor-credentials', usernameVariable: 'HARBOR_USER', passwordVariable: 'HARBOR_PASS')]) {
                        script {
                            def auth = "${HARBOR_USER}:${HARBOR_PASS}".bytes.encodeBase64().toString()
                            def dockerConfig = """
                            {
                                "auths": {
                                    "${HARBOR_URL}": {
                                        "auth": "${auth}"
                                    }
                                }
                            }
                            """
                            writeFile file: '/kaniko/.docker/config.json', text: dockerConfig
                        }

                        // --- THIS IS THE CORRECTED LINE ---
                        // The entire command is now on a single line, with no backslashes.
                        sh " /kaniko/executor --context=`pwd` --dockerfile=src/RogueLearn.User.Api/Dockerfile --destination=${HARBOR_IMAGE_PATH}:${env.GIT_COMMIT.substring(0,8)} --destination=${HARBOR_IMAGE_PATH}:latest "
                    }
                }
            }
        }

        stage('Update K8s Manifests') {
            when { branch 'main' }
            agent {
                 kubernetes {
                    yaml """
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: kustomize
    image: alpine/k8s:1.28.4
    command: [sleep]
    args: [99d]
"""
                }
            }
            steps {
                container('kustomize') {
                    withCredentials([string(credentialsId: 'github-token', variable: 'GIT_TOKEN')]) {
                        sh """
                        git clone https://oauth2:${GIT_TOKEN}@github.com/FA25SE050-RogueLearn/RogueLearn.Kubernetes.git k8s-manifests
                        cd k8s-manifests
                        git config user.name "Jenkins CI"
                        git config user.email "jenkins@your-domain.com"
                        cd roguelearn-user-api/base
                        
                        kustomize edit set image ${KUSTOMIZE_BASE_IMAGE}=${HARBOR_IMAGE_PATH}:${env.GIT_COMMIT.substring(0,8)}
                        
                        cd ../..
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
        always {
            echo 'Pipeline finished.'
            deleteDir()
        }
    }
}