export interface Product {
  id:           number;
  name:         string;
  description:  string;
  price:        number;
  stock:        number;
  imageUrl:     string;
  brand:        string;
  categoryName: string;
  categoryId:   number;
}

export interface Category {
  id:    number;
  name:  string;
  slug:  string;
  count: number;
}

export interface CartItem {
  productId:   string;
  productName: string;
  unitPrice:   number;
  quantity:    number;
  imageUrl:    string;
}

export interface Cart {
  sessionId: string;
  items:     CartItem[];
  total:     number;
  count:     number;
}

export interface ProductsResponse {
  total:    number;
  page:     number;
  pageSize: number;
  items:    Product[];
}

export interface OrderResponse {
  orderId:   number;
  status:    string;
  total:     number;
  itemCount: number;
  message:   string;
}
