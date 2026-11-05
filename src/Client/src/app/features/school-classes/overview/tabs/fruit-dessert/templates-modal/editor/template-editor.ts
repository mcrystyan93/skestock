import { afterNextRender, Component, DestroyRef, effect, inject, input, model, signal, untracked, viewChild, ElementRef } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import { MessagePlaceholderDto } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';

/** Server limit of the rendered message (FruitDessertMessage.Body); the counter is only an approximation of it. */
export const MAX_RENDERED_LENGTH = 2000;

type Mark = 'bold' | 'italic' | 'strike' | 'code' | 'bulletList';

@Component({
  selector: 'ske-fruit-dessert-template-editor',
  templateUrl: './template-editor.html',
  imports: [
    NzButtonComponent,
    NzDropdownDirective,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzTooltipDirective
  ]
})
export class TemplateEditor implements FormValueControl<string> {
  public readonly value = model<string>('');
  public readonly placeholders = input<MessagePlaceholderDto[]>([]);

  public readonly limit = MAX_RENDERED_LENGTH;
  public readonly editor = signal<Editor | null>(null);
  public readonly textLength = signal(0);
  // Bumped on every transaction so the toolbar's active states re-evaluate.
  private readonly _transactions = signal(0);

  private readonly _host = viewChild.required<ElementRef<HTMLElement>>('host');

  public constructor() {
    const destroyRef = inject(DestroyRef);
    afterNextRender(() => {
      const editor = new Editor({
        element: this._host().nativeElement,
        content: this.value(),
        // Only the subset WhatsApp can express; the server sanitizer is the real gate.
        extensions: [
          StarterKit.configure({
            heading: false,
            blockquote: false,
            codeBlock: false,
            horizontalRule: false,
            orderedList: false,
            link: false,
            underline: false,
            trailingNode: false
          })
        ],
        onTransaction: () => this._transactions.update(n => n + 1),
        onUpdate: ({ editor }) => {
          this.textLength.set(editor.getText().length);
          this.value.set(editor.isEmpty ? '' : editor.getHTML());
        }
      });
      this.textLength.set(editor.getText().length);
      this.editor.set(editor);
      destroyRef.onDestroy(() => editor.destroy());
    });

    // External value changes (form reset, load); the guard prevents echoing our own updates back.
    effect(() => {
      const value = this.value();
      const editor = this.editor();
      if (!editor) return;
      untracked(() => {
        if (value === (editor.isEmpty ? '' : editor.getHTML())) return;
        editor.commands.setContent(value, { emitUpdate: false });
        this.textLength.set(editor.getText().length);
      });
    });
  }

  public isActive(mark: Mark): boolean {
    this._transactions();
    return this.editor()?.isActive(mark) ?? false;
  }

  public toggle(mark: Mark): void {
    const chain = this.editor()?.chain().focus();
    if (!chain) return;
    switch (mark) {
      case 'bold': chain.toggleBold().run(); break;
      case 'italic': chain.toggleItalic().run(); break;
      case 'strike': chain.toggleStrike().run(); break;
      case 'code': chain.toggleCode().run(); break;
      case 'bulletList': chain.toggleBulletList().run(); break;
    }
  }

  public insertPlaceholder(token: string): void {
    this.editor()?.chain().focus().insertContent(token).run();
  }
}
