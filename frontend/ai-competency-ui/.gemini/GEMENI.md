# Angular Project Rules

## Component Structure

When generating Angular components:

- NEVER use inline templates.
- NEVER use inline styles.

Always generate the following files:

- `*.component.ts`
- `*.component.html`
- `*.component.scss`

Example:

```ts
@Component({
  selector: 'app-example',
  standalone: true,
  templateUrl: './example.component.html',
  styleUrls: ['./example.component.scss']
})
export class ExampleComponent {}