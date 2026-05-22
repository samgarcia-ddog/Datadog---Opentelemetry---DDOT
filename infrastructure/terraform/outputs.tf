output "resource_group_name" {
  description = "Nombre del Resource Group"
  value       = azurerm_resource_group.main.name
}

output "aks_name" {
  description = "Nombre del cluster AKS"
  value       = azurerm_kubernetes_cluster.main.name
}

output "aks_id" {
  description = "ID del cluster AKS"
  value       = azurerm_kubernetes_cluster.main.id
}

output "acr_name" {
  description = "Nombre del ACR"
  value       = azurerm_container_registry.main.name
}

output "acr_login_server" {
  description = "URL del ACR (para docker push/pull y manifiestos K8s)"
  value       = azurerm_container_registry.main.login_server
}

output "subscription_id" {
  description = "Azure Subscription ID"
  value       = var.subscription_id
}

output "kubeconfig_command" {
  description = "Comando para configurar kubectl"
  value       = "az aks get-credentials --resource-group ${azurerm_resource_group.main.name} --name ${azurerm_kubernetes_cluster.main.name} --subscription ${var.subscription_id} --overwrite-existing"
}

output "location" {
  description = "Azure region"
  value       = azurerm_resource_group.main.location
}
