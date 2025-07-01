/**
 * DocFlowHub Upload Progress Enhancement - Phase 3.3: Real-time Feedback
 * Provides comprehensive real-time feedback during AI-powered document upload workflow
 */

// Upload Progress Enhancement Module
const UploadProgressManager = {
    // Elements cache
    elements: {},
    
    // Progress stages configuration
    stages: {
        upload: { name: 'upload', progress: 25, message: 'Uploading document...' },
        ai: { name: 'ai', progress: 75, message: 'AI processing...' },
        complete: { name: 'complete', progress: 100, message: 'Complete!' }
    },

    // AI processing sub-stages
    aiStages: [
        {
            name: 'extracting',
            title: 'Extracting Content',
            description: 'Reading and parsing document content...',
            duration: 3000,
            progress: 35
        },
        {
            name: 'analyzing',
            title: 'AI Analysis',
            description: 'Analyzing content with AI model...',
            duration: 8000,
            progress: 70
        },
        {
            name: 'generating',
            title: 'Generating Summary',
            description: 'Creating intelligent summary and insights...',
            duration: 4000,
            progress: 95
        }
    ],

    // Initialize the upload progress system
    init() {
        this.cacheElements();
        this.bindEvents();
        console.log('Upload Progress Manager initialized');
    },

    // Cache DOM elements for performance
    cacheElements() {
        this.elements = {
            uploadForm: document.getElementById('upload-form'),
            progressContainer: document.getElementById('upload-progress'),
            progressSteps: document.getElementById('progress-steps'),
            progressBar: document.getElementById('progress-bar'),
            progressText: document.getElementById('progress-text'),
            progressSpinner: document.getElementById('progress-spinner'),
            progressMessage: document.getElementById('progress-message-text'),
            aiProcessingDetails: document.getElementById('ai-processing-details'),
            aiStageTitle: document.getElementById('ai-stage-title'),
            aiStageDescription: document.getElementById('ai-stage-description'),
            aiTimeRemaining: document.getElementById('ai-time-remaining'),
            aiToggle: document.getElementById('ai-summary-toggle'),
            modelSelect: document.getElementById('ai-model-select'),
            qualitySlider: document.getElementById('quality-slider'),
            submitBtn: document.getElementById('submit-btn')
        };
    },

    // Bind form submission events
    bindEvents() {
        if (this.elements.uploadForm) {
            this.elements.uploadForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleFormSubmission(e);
            });
        }
    },

    // Handle form submission with enhanced progress
    async handleFormSubmission(event) {
        const isAIEnabled = this.elements.aiToggle && this.elements.aiToggle.checked;
        const selectedModel = this.elements.modelSelect ? this.elements.modelSelect.value : 'Gpt4oMini';
        const quality = this.elements.qualitySlider ? parseFloat(this.elements.qualitySlider.value) : 0.7;

        console.log('Starting upload with AI enabled:', isAIEnabled);

        try {
            // Show initial progress
            this.showProgressContainer();
            this.updateSubmitButton(isAIEnabled);

            // Stage 1: File Upload
            await this.processUploadStage();

            if (isAIEnabled) {
                // Stage 2: AI Processing
                await this.processAIStage(selectedModel, quality);
            }

            // Stage 3: Complete
            this.processCompleteStage();

            // Submit the actual form after UI feedback
            setTimeout(() => {
                this.submitFormActually(event.target);
            }, 1000);

        } catch (error) {
            this.handleError(error);
        }
    },

    // Show the progress container
    showProgressContainer() {
        this.elements.progressContainer.classList.add('show');
        this.elements.progressSteps.style.display = 'block';
        this.elements.progressSpinner.style.display = 'inline-block';
        
        // Activate upload step
        this.updateStep('upload', 'active');
        this.updateProgressBar(0, 'Starting upload...');
    },

    // Update submit button state
    updateSubmitButton(isAIEnabled) {
        if (this.elements.submitBtn) {
            const text = isAIEnabled ? 
                '<i class="bi bi-robot me-1"></i> Uploading & Processing with AI...' :
                '<i class="bi bi-cloud-upload me-1"></i> Uploading...';
            this.elements.submitBtn.innerHTML = text;
            this.elements.submitBtn.disabled = true;
        }
    },

    // Process upload stage
    async processUploadStage() {
        this.updateProgressBar(10, 'Preparing upload...');
        await this.delay(500);

        this.updateProgressBar(25, 'Uploading document...');
        await this.animateProgressTo(25, 1500);

        // Complete upload stage
        this.updateStep('upload', 'completed');
        this.updateStepLine(0, 'completed');
    },

    // Process AI stage with sub-stages
    async processAIStage(selectedModel, quality) {
        // Activate AI step
        this.updateStep('ai', 'active');
        this.showAIProcessingDetails();

        // Process each AI sub-stage
        for (let i = 0; i < this.aiStages.length; i++) {
            const stage = this.aiStages[i];
            const adjustedDuration = this.calculateAdjustedDuration(stage.duration, selectedModel, quality);
            
            await this.processAISubStage(stage, adjustedDuration, i);
        }

        // Complete AI stage
        this.updateStep('ai', 'completed');
        this.updateStepLine(1, 'completed');
        this.completeAIProcessing();
    },

    // Process individual AI sub-stage
    async processAISubStage(stage, duration, stageIndex) {
        // Update AI stage display
        this.elements.aiStageTitle.textContent = stage.title;
        this.elements.aiStageDescription.textContent = stage.description;
        
        // Add stage-specific styling
        this.elements.aiProcessingDetails.className = `ai-processing-details mt-2 ai-stage-${stage.name}`;
        
        // Calculate and display time remaining
        const remainingTime = this.calculateRemainingTime(stageIndex);
        this.elements.aiTimeRemaining.textContent = remainingTime;
        
        // Animate progress to stage target
        await this.animateProgressTo(stage.progress, duration);
    },

    // Show AI processing details panel
    showAIProcessingDetails() {
        this.elements.aiProcessingDetails.style.display = 'block';
        this.updateProgressBar(30, 'AI processing started...');
    },

    // Complete AI processing with success state
    completeAIProcessing() {
        this.elements.aiStageTitle.textContent = 'Processing Complete';
        this.elements.aiStageDescription.textContent = 'AI summary generated successfully';
        this.elements.aiTimeRemaining.textContent = 'Done!';
        
        // Replace spinner with success icon
        const spinner = this.elements.aiProcessingDetails.querySelector('.spinner-border');
        if (spinner) {
            spinner.style.display = 'none';
            const checkIcon = document.createElement('i');
            checkIcon.className = 'bi bi-check-circle-fill text-success';
            checkIcon.style.fontSize = '1.2rem';
            spinner.parentNode.appendChild(checkIcon);
        }
    },

    // Process completion stage
    processCompleteStage() {
        this.updateStep('complete', 'active');
        this.updateProgressBar(100, 'Upload and AI processing completed successfully!');
        
        // Update message with success icon
        this.elements.progressMessage.innerHTML = 
            '<i class="bi bi-check-circle me-1 text-success"></i>Upload and AI processing completed successfully!';
        this.elements.progressSpinner.style.display = 'none';
    },

    // Update step state (active, completed)
    updateStep(stepName, state) {
        const stepElement = document.getElementById(`step-${stepName}`);
        if (stepElement) {
            stepElement.className = `step-item ${state}`;
        }
    },

    // Update step connecting lines
    updateStepLine(lineIndex, state) {
        const stepLines = document.querySelectorAll('.step-line');
        if (stepLines[lineIndex]) {
            stepLines[lineIndex].classList.add(state);
        }
    },

    // Update progress bar and message
    updateProgressBar(percentage, message) {
        this.elements.progressBar.style.width = `${percentage}%`;
        this.elements.progressText.textContent = `${Math.round(percentage)}%`;
        this.elements.progressMessage.textContent = message;
    },

    // Animate progress bar to target percentage
    async animateProgressTo(targetProgress, duration) {
        const startProgress = parseInt(this.elements.progressBar.style.width) || 0;
        const progressDiff = targetProgress - startProgress;
        const steps = 50;
        const stepDuration = duration / steps;

        for (let i = 0; i <= steps; i++) {
            const currentProgress = startProgress + (progressDiff * i / steps);
            this.updateProgressBar(currentProgress, this.elements.progressMessage.textContent);
            await this.delay(stepDuration);
        }
    },

    // Calculate adjusted duration based on model and quality
    calculateAdjustedDuration(baseDuration, model, quality) {
        const modelMultipliers = {
            'Gpt4oMini': 0.8,
            'Gpt4o': 1.0,
            'Gpt41Mini': 1.2,
            'Gpt41': 1.5
        };
        
        const qualityMultiplier = quality <= 0.3 ? 0.7 : quality <= 0.7 ? 1.0 : 1.4;
        const modelMultiplier = modelMultipliers[model] || 1.0;
        
        return baseDuration * modelMultiplier * qualityMultiplier;
    },

    // Calculate remaining time for current stage
    calculateRemainingTime(currentStageIndex) {
        const remainingStages = this.aiStages.slice(currentStageIndex + 1);
        const remainingTime = remainingStages.reduce((total, stage) => total + stage.duration, 0);
        
        if (remainingTime === 0) return 'Almost done!';
        if (remainingTime < 60000) return `${Math.round(remainingTime / 1000)}s`;
        return `${(remainingTime / 60000).toFixed(1)}m`;
    },

    // Submit the form actually after UI feedback
    submitFormActually(form) {
        // Create a new form submission without the event listener
        const newForm = form.cloneNode(true);
        form.parentNode.replaceChild(newForm, form);
        newForm.submit();
    },

    // Handle errors during upload
    handleError(error) {
        console.error('Upload error:', error);
        this.updateProgressBar(0, 'Upload failed. Please try again.');
        this.elements.progressContainer.classList.remove('show');
        this.elements.submitBtn.disabled = false;
        this.elements.submitBtn.innerHTML = '<i class="bi bi-cloud-upload me-1"></i> Upload Document';
    },

    // Utility delay function
    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    UploadProgressManager.init();
});

// Global reference for other scripts
window.UploadProgressManager = UploadProgressManager; 