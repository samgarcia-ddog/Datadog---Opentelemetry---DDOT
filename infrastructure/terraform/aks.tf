resource "azurerm_kubernetes_cluster" "main" {
  name                = var.cluster_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  dns_prefix          = var.cluster_name
  kubernetes_version  = var.kubernetes_version

  # Managed identity para el control plane
  identity {
    type = "SystemAssigned"
  }

  # Pool de sistema (nodos para kube-system, etc.)
  default_node_pool {
    name                = "system"
    node_count          = var.system_node_count
    vm_size             = var.system_node_vm_size
    os_disk_size_gb     = 50
    vnet_subnet_id      = azurerm_subnet.aks.id
    only_critical_addons_enabled = true  # Solo workloads críticos en este pool

    upgrade_settings {
      max_surge = "10%"
    }
  }

  # Networking: Azure CNI Overlay (moderno, más eficiente en IPs)
  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = "overlay"
    network_policy      = "cilium"
    network_data_plane  = "cilium"
    load_balancer_sku   = "standard"
    pod_cidr            = "192.168.0.0/16"
    service_cidr        = "10.0.0.0/16"
    dns_service_ip      = "10.0.0.10"
  }

  # OIDC + Workload Identity (para federated credentials si se necesitan)
  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  # Monitoreo básico de Azure (opcional, puede desactivarse si usamos solo Datadog)
  monitor_metrics {}

  # HTTP Application Routing desactivado (usamos nuestro propio ingress-nginx)
  http_application_routing_enabled = false

  tags = var.tags
}

# Pool de usuario para las workloads de la app
resource "azurerm_kubernetes_cluster_node_pool" "app" {
  name                  = "apppool"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.main.id
  vm_size               = var.app_node_vm_size
  node_count            = var.app_node_count
  os_disk_size_gb       = 50
  vnet_subnet_id        = azurerm_subnet.aks.id
  mode                  = "User"

  node_labels = {
    "workload-type" = "application"
  }

  upgrade_settings {
    max_surge = "1"
  }

  tags = var.tags
}

# Asignar rol AcrPull al kubelet identity del AKS sobre el ACR
resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                = azurerm_container_registry.main.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
}
