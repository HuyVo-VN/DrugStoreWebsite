import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';

@Pipe({
  name: 'markdown',
  standalone: true
})
export class MarkdownPipe implements PipeTransform {

  private sanitizer = inject(DomSanitizer);

  transform(value: string): SafeHtml {
    if (!value) return ''; // Nếu AI thực sự gửi chuỗi rỗng

    try {
      // Ép kiểu render Markdown
      const rawHtml = marked.parse(value) as string;
      return this.sanitizer.bypassSecurityTrustHtml(rawHtml);
    } catch (error) {
      // Nếu Markdown render lỗi, trả về nguyên bản chữ thô (tránh bị trắng bóc)
      return value;
    }
  }

}
