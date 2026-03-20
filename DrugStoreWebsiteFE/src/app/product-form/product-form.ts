import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule],
  templateUrl: './product-form.html',
  styleUrls: ['./product-form.css']
})
export class ProductForm implements OnInit {
  productForm!: FormGroup;
  isEditMode = false;
  selectedImageFile: File | null = null;
  imagePreview: string = '/images/default-product.png';
  isImageRemoved = false;
  categories: any[] = []; // Sẽ nhận từ component cha truyền vào

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<ProductForm>,
    @Inject(MAT_DIALOG_DATA) public data: any // Nhận dữ liệu từ product.ts truyền sang
  ) { }

  ngOnInit() {
    this.categories = this.data.categories || [];
    this.isEditMode = !!this.data.product;

    // 1. KHỞI TẠO FORM BẰNG REACTIVE FORMS CỰC KỲ CLEAN
    this.productForm = this.fb.group({
      name: ['', Validators.required],
      categoryId: ['', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      stock: [0, [Validators.required, Validators.min(0)]],
      discountPercent: [0, [Validators.min(0), Validators.max(100)]],
      discountEndDate: [''],
      saleStock: [0],
      description: [''],
      specifications: this.fb.array([]) // <-- QUẢN LÝ MẢNG JSON ĐỘNG Ở ĐÂY
    });

    // 2. NẾU LÀ CHẾ ĐỘ SỬA -> ĐỔ DỮ LIỆU CŨ VÀO FORM
    if (this.isEditMode) {
      const p = this.data.product;
      this.productForm.patchValue({
        name: p.name,
        categoryId: p.categoryId,
        price: p.price,
        stock: p.stock,
        discountPercent: p.discountPercent || 0,
        discountEndDate: p.discountEndDate ? p.discountEndDate.substring(0, 16) : '',
        saleStock: p.saleStock || 0,
        description: p.description
      });

      if (p.imageUrl) {
        this.imagePreview = p.imageUrl.startsWith('http') ? p.imageUrl : `https://localhost:5287${p.imageUrl}`;
      }

      // Đổ mảng Specifications cũ vào (nếu có)
      if (p.specifications && p.specifications !== "[]") {
        try {
          const parsedSpecs = JSON.parse(p.specifications);
          parsedSpecs.forEach((spec: any) => this.addSpecification(spec.key, spec.value));
        } catch (e) { }
      }
    }
  }

  // --- CÁC HÀM QUẢN LÝ MẢNG THÔNG SỐ (SPECIFICATIONS) ---
  get specifications() {
    return this.productForm.get('specifications') as FormArray;
  }

  addSpecification(key: string = '', value: string = '') {
    const specGroup = this.fb.group({
      key: [key, Validators.required],
      value: [value, Validators.required]
    });
    this.specifications.push(specGroup);
  }

  removeSpecification(index: number) {
    this.specifications.removeAt(index);
  }

  // --- QUẢN LÝ HÌNH ẢNH ---
  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedImageFile = file;
      this.isImageRemoved = false;
      const reader = new FileReader();
      reader.onload = (e: any) => this.imagePreview = e.target.result;
      reader.readAsDataURL(file);
    }
  }

  removeImage() {
    this.selectedImageFile = null;
    this.imagePreview = '/images/default-product.png';
    this.isImageRemoved = true;
  }

  // --- HÀM LƯU GỬI DỮ LIỆU VỀ CHO COMPONENT CHA ---
  onSubmit() {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched(); // Hiện lỗi màu đỏ nếu chưa nhập đủ
      return;
    }

    const formValues = this.productForm.value;

    // Gom tất cả lại thành một Object gọn gàng để trả về cho product.ts gọi API
    const resultData = {
      ...formValues,
      imageFile: this.selectedImageFile,
      isImageRemoved: this.isImageRemoved,
      // Ép mảng specifications thành chuỗi JSON luôn
      specificationsJson: JSON.stringify(formValues.specifications)
    };

    // Đóng Popup và ném dữ liệu về
    this.dialogRef.close(resultData);
  }
}
