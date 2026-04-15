import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { environment } from '../../../../environments/environment';
import { AiService } from '../../../core/ai/ai.service';

interface GeneratedQuestion {
  question: string;
  options: string[];
  answer: string;
  explanation: string;
}

@Component({
  selector: 'app-question-generator',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './question-generator.component.html',
  styleUrl: './question-generator.component.scss'
})
export class QuestionGeneratorComponent {
   private aiService = inject(AiService);
   private fb = inject(FormBuilder);
   
   isGenerating = false;
   questions = signal<any[]>([]);

   form = this.fb.group({
     mix: this.fb.array([this.createMixItem()])
   });

   get mixControls() {
     return (this.form.get('mix') as FormArray).controls;
   }

   createMixItem() {
     return this.fb.group({
       topic: ['', Validators.required],
       group: ['NhanThucAI', Validators.required],
       level: ['NenTang', Validators.required],
       type: ['mcq_single', Validators.required],
       count: [5, [Validators.required, Validators.min(1), Validators.max(20)]]
     });
   }

   addMixItem() {
     (this.form.get('mix') as FormArray).push(this.createMixItem());
   }

   removeMixItem(index: number) {
     (this.form.get('mix') as FormArray).removeAt(index);
   }

    generate() {
      if (this.form.invalid) return;

      this.isGenerating = true;
      this.questions.set([]);

      // Map form value to GenerationPlan
      const formValue = this.form.value;
      const mixItems = formValue.mix || [];
      const plan = {
        total: mixItems.reduce((sum: number, item: any) => sum + (item.count || 0), 0),
        mix: mixItems.map((item: any) => ({
          ...item,
          difficulty: [2], // Default difficulty
        }))
      };

      this.aiService.generateAgentic(plan).subscribe({
        next: (res) => {
          // The API returns { questions: [], summary: {}, audit: [] }
          if (res && res.questions) {
             this.questions.set(res.questions);
          } else {
             this.questions.set([]);
          }
          this.isGenerating = false;
        },
        error: (err: any) => {
          console.error(err);
          this.isGenerating = false;
          alert('Failed to generate agentic questions. Ensure backend services are running.');
        }
      });
    }

   saveAllToBank() {
     const questionsToSave = this.questions();
     if (!questionsToSave || questionsToSave.length === 0) return;

     this.aiService.saveQuestions(questionsToSave).subscribe({
       next: (res: any) => {
         alert(`Successfully saved ${res.count} questions to the bank!`);
         this.questions.set([]); // clear after saving
       },
       error: (err: any) => {
         console.error('Failed to save questions = ', err);
         alert('An error occurred while saving the questions.');
       }
     });
   }
}
