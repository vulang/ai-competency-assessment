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
       group: ['Fundamental', Validators.required],
       level: ['Basic', Validators.required],
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

   autoSuggestPlan() {
     const suggestedMix = [
       // 1. Fundamental
       { topic: 'AI vs ML vs DL', group: 'Fundamental', level: 'Basic', type: 'mcq_single', count: 2 },
       { topic: 'AI Training Lifecycle', group: 'Fundamental', level: 'Intermediate', type: 'mcq_multi', count: 2 },
       { topic: 'Model Architecture Trade-offs', group: 'Fundamental', level: 'Advanced', type: 'scenario', count: 1 },
       
       // 2. Data
       { topic: 'Data Types and Sources', group: 'Data', level: 'Basic', type: 'tf', count: 2 },
       { topic: 'Data Quality and EDA', group: 'Data', level: 'Intermediate', type: 'short', count: 2 },
       { topic: 'Data Pipelines in Production', group: 'Data', level: 'Advanced', type: 'scenario', count: 1 },
       
       // 3. Critical Thinking
       { topic: 'AI Hallucination vs Facts', group: 'Critical Thinking', level: 'Basic', type: 'tf', count: 2 },
       { topic: 'Fact-checking and Verification', group: 'Critical Thinking', level: 'Intermediate', type: 'mcq_multi', count: 2 },
       { topic: 'Designing Verification Frameworks', group: 'Critical Thinking', level: 'Advanced', type: 'scenario', count: 1 },
       
       // 4. AI Use Cases
       { topic: 'Coding Assistants (Copilot, Cursor)', group: 'AI Use Cases', level: 'Basic', type: 'mcq_single', count: 2 },
       { topic: 'RAG and API Integration', group: 'AI Use Cases', level: 'Intermediate', type: 'mcq_multi', count: 2 },
       { topic: 'Designing Multi-agent Systems', group: 'AI Use Cases', level: 'Advanced', type: 'scenario', count: 1 },
       
       // 5. AI Ethics
       { topic: 'AI Risks and Principles', group: 'AI Ethics', level: 'Basic', type: 'tf', count: 2 },
       { topic: 'Bias Mitigation and Privacy', group: 'AI Ethics', level: 'Intermediate', type: 'short', count: 2 },
       { topic: 'AI Policy and Governance', group: 'AI Ethics', level: 'Advanced', type: 'scenario', count: 1 },
       
       // 6. AI Tools
       { topic: 'Prompt Engineering Basics', group: 'AI Tools', level: 'Basic', type: 'mcq_single', count: 2 },
       { topic: 'Advanced Prompting and Tools', group: 'AI Tools', level: 'Intermediate', type: 'mcq_multi', count: 2 },
       { topic: 'Automated Workflows and MCP', group: 'AI Tools', level: 'Advanced', type: 'scenario', count: 1 },
       
       // 7. Future of Work
       { topic: 'Automating vs Augmenting', group: 'Future of Work', level: 'Basic', type: 'mcq_single', count: 2 },
       { topic: 'Upskilling Strategies', group: 'Future of Work', level: 'Intermediate', type: 'short', count: 2 },
       { topic: 'Workforce Transformation', group: 'Future of Work', level: 'Advanced', type: 'scenario', count: 1 }
     ];

     const mixArray = this.form.get('mix') as FormArray;
     mixArray.clear();
     
     suggestedMix.forEach(item => {
       mixArray.push(this.fb.group({
         topic: [item.topic, Validators.required],
         group: [item.group, Validators.required],
         level: [item.level, Validators.required],
         type: [item.type, Validators.required],
         count: [item.count, [Validators.required, Validators.min(1), Validators.max(20)]]
       }));
     });
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
